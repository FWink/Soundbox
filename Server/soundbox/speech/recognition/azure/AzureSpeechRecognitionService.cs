﻿using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Soundbox.Audio;
using Soundbox.Audio.Processing;
using Soundbox.Audio.Processing.Noisegate;
using Soundbox.Speech.Recognition.AppSettings;
using Soundbox.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Speech.Recognition.Azure
{
    /// <summary>
    /// <see cref="ISpeechRecognitionService"/> that uses Microsoft Azure's "Cognitive Speech" speech-to-text services.
    /// </summary>
    public class AzureSpeechRecognitionService : ISpeechRecognitionService
    {
        protected ILogger Logger;
        protected IServiceProvider ServiceProvider;

        #region "Config/Settings"

        /// <summary>
        /// Static app settings (API keys and such)
        /// </summary>
        protected SpeechRecognitionAppSettings Settings;

        /// <summary>
        /// Config for this instance (e.g., audio)
        /// </summary>
        protected SpeechRecognitionConfig Config;

        #region "Credentials"

        /// <summary>
        /// The API credentials that the next <see cref="Start(SpeechRecognitionOptions)"/> call should use.
        /// </summary>
        protected AppSettings.Azure.AzureSpeechRecognitionCredentials Credentials => Settings.Providers.Azure.Credentials[CredentialsIndex];

        /// <summary>
        /// Current index in <see cref="AppSettings.Azure.AzureSpeechRecognitionProviderAppSettings.Credentials"/>
        /// </summary>
        private int CredentialsIndex;

        #endregion

        #endregion

        /// <summary>
        /// Lock used for the public <see cref="Start(SpeechRecognitionOptions)"/>, <see cref="Stop"/> and <see cref="UpdateOptions(SpeechRecognitionOptions)"/>
        /// methods (not recursive!)
        /// </summary>
        protected readonly AsyncLock Lock = new AsyncLock();

        /// <summary>
        /// True: recognition is currently active.
        /// </summary>
        protected bool IsRunning;
        protected SpeechRecognizer SpeechRecognizer;

        /// <summary>
        /// Audio config created in <see cref="SetConfig(SpeechRecognitionConfig)"/>
        /// </summary>
        protected AudioConfig AudioConfig;
        /// <summary>
        /// Optional: if <see cref="SpeechRecognizer"/> is fed from this, then we need to <see cref="IStreamAudioSource.Start"/> and <see cref="IStreamAudioSource.Stop"/> this as required.
        /// </summary>
        protected IStreamAudioSource StreamAudioSource;
        /// <summary>
        /// True: <see cref="StreamAudioSource"/> should be running right now.
        /// </summary>
        protected bool StreamAudioRunning;
        /// <summary>
        /// Optional: noise gate for <see cref="StreamAudioSource"/>. We call <see cref="INoiseGateStreamAudioProcessor.OnAudioStop"/> when the speech recognizer detects a speech-stopped event.
        /// Does not need to be disposed explicitly.
        /// </summary>
        protected INoiseGateStreamAudioProcessor StreamAudioNoiseGate;

        /// <summary>
        /// Disposed both in <see cref="Dispose"/> but also in <see cref="Stop"/>: disposables for each run.
        /// </summary>
        protected ICollection<IDisposable> Disposables = new List<IDisposable>();

        public AzureSpeechRecognitionService(ILogger<AzureSpeechRecognitionService> logger, IServiceProvider serviceProvider)
        {
            this.Logger = logger;
            this.ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Sets the recognizer's config. For internal usage only.
        /// </summary>
        /// <param name="config"></param>
        /// <returns>
        /// False: the given config is not compatible.
        /// </returns>
        internal bool SetConfig(SpeechRecognitionConfig config, SpeechRecognitionAppSettings settings)
        {
            this.Settings = settings;
            this.Config = config;
            this.AudioConfig = GetAudioConfig();

            return this.AudioConfig != null;
        }

        public async Task Start(SpeechRecognitionOptions options)
        {
            using (await Lock.LockAsync())
            {
                await StartInt(options);
            }
        }

        /// <summary>
        /// Implements <see cref="Start(SpeechRecognitionOptions)"/>. Must hold <see cref="Lock"/>
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        protected async Task StartInt(SpeechRecognitionOptions options)
        {
            SpeechRecognizer recognizer = null;
            ICollection<IDisposable> disposables = new List<IDisposable>();

            try
            {
                Logger.LogInformation("Starting speech recognition");

                var credentials = this.Credentials;

                var speechConfig = SpeechConfig.FromEndpoint(new Uri($"wss://{credentials.Region}.stt.speech.microsoft.com/speech/universal/v2"), credentials.SubscriptionKey);
                speechConfig.SetProfanity(ProfanityOption.Raw);

                if (options.Languages.Count > 1)
                {
                    //enable continuous language detection when we have more than 1 language
                    //this seems kind of buggy though, at times the speech recognition just simply doesn't work at all when this is enabled
                    speechConfig.SetProperty(PropertyId.SpeechServiceConnection_ContinuousLanguageIdPriority, "Latency");
                }

                var languageConfig = AutoDetectSourceLanguageConfig.FromLanguages(options.Languages.Select(lang =>
                {
                    //convert language selections
                    if (lang.Length == 2)
                    {
                        //two-letter code. select some default five-letter code instead.
                        if (lang == "en")
                            lang = "en-US";
                        else
                            lang = lang + "-" + lang.ToUpperInvariant();
                    }
                    return lang;
                }).ToArray());

                recognizer = new SpeechRecognizer(speechConfig, languageConfig, AudioConfig);
                disposables.Add(recognizer);

                //set up the special phrases if any
                if (options.Phrases?.Count > 0)
                {
                    var phrases = PhraseListGrammar.FromRecognizer(recognizer);
                    foreach (var phrase in options.Phrases)
                    {
                        phrases.AddPhrase(phrase);
                    }
                }

                //prepare events
                recognizer.Canceled += (sender, e) =>
                {
                    Dispose(ref disposables);

                    bool restart = false;
                    bool log = true;

                    if (e.ErrorCode == CancellationErrorCode.Forbidden || e.ErrorCode == CancellationErrorCode.AuthenticationFailure ||
                            (e.ErrorCode == CancellationErrorCode.BadRequest && e.ErrorDetails.Contains("Quota exceeded")))
                    {
                        //out of quota (or invalid key, try the next one anyway)
                        int credentialsIndexCurrent = CredentialsIndex;
                        if (NextCredentials())
                        {
                            Logger.LogInformation($"Out of quota for credentials {credentialsIndexCurrent}. Restarting with {CredentialsIndex}");
                            restart = true;
                            log = false;
                        }
                    }
                    else if (e.ErrorCode == CancellationErrorCode.ServiceTimeout)
                    {
                        //timeout, probably due to inactivity
                        restart = true;
                    }
                    //TODO speech errors ConnectionFailure and ServiceUnavailable => exponential backoff

                    if (e.Reason == CancellationReason.EndOfStream || e.Reason == CancellationReason.CancelledByUser)
                    {
                        log = false;
                    }

                    if (log)
                    {
                        Logger.LogWarning($"Recognition stopped. reason={e.Reason}, errorCode={e.ErrorCode}, details={e.ErrorDetails}, restarting={restart}");
                    }

                    if (restart)
                    {
                        Threading.Tasks.FireAndForget(() => Start(options));
                        return;
                    }

                    Stopped?.Invoke(this, new SpeechRecognitionStoppedEvent()
                    {
                        Message = $"{e.ErrorCode}: {e.ErrorDetails}"
                    });
                };
                recognizer.Recognizing += (sender, e) =>
                {
                    OnSpeechEvent(e, false);
                };
                recognizer.Recognized += (sender, e) =>
                {
                    OnSpeechEvent(e, true);
                };
                recognizer.SpeechEndDetected += (sender, e) =>
                {
                    StreamAudioNoiseGate?.OnAudioStop();
                };

                //start recognizing
                await recognizer.StartContinuousRecognitionAsync();

                //start our audio source
                if (StreamAudioSource != null && !StreamAudioRunning)
                {
                    await StreamAudioSource.Start();
                    StreamAudioRunning = true;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Could not start continuous recognition");

                recognizer?.Dispose();
                throw;
            }

            SpeechRecognizer = recognizer;
            IsRunning = true;

            this.Disposables = disposables;
        }

        public async Task Stop()
        {
            using (await Lock.LockAsync())
            {
                await StopInt(true);
            }
        }

        /// <summary>
        /// Implements <see cref="Stop"/>. Must hold <see cref="Lock"/>
        /// </summary>
        /// <param name="withEvent">
        /// False: <see cref="Stopped"/> is not triggered.
        /// </param>
        /// <returns></returns>
        protected async Task StopInt(bool withEvent)
        {
            if (StreamAudioSource != null && StreamAudioRunning)
            {
                await StreamAudioSource.Stop();
                StreamAudioRunning = false;
            }

            var recognizer = SpeechRecognizer;
            if (recognizer != null)
            {
                Logger.LogInformation("Stopping speech recognition");

                //simply disposing the speechRecognizer takes 10s
                await recognizer.StopContinuousRecognitionAsync();

                IsRunning = false;
                Dispose(ref Disposables);

                if (withEvent)
                {
                    Stopped?.Invoke(this, new SpeechRecognitionStoppedEvent());
                }
            }
        }

        public async Task UpdateOptions(SpeechRecognitionOptions options)
        {
            using (await Lock.LockAsync())
            {
                if (!IsRunning)
                {
                    //nothing to do
                    return;
                }

                Logger.LogInformation("UpdateOptions");

                try
                {
                    await StopInt(false);
                    await StartInt(options);
                }
                catch (Exception e)
                {
                    //could not restart
                    Logger.LogError(e, "Could not restart recognition from UpdateOptions");

                    Stopped?.Invoke(this, new SpeechRecognitionStoppedEvent()
                    {
                        Exception = e
                    });
                    return;
                }
            }
        }

        #region "Events"

        public event EventHandler<SpeechRecognizedEvent> Recognized;
        public event EventHandler<SpeechRecognitionStoppedEvent> Stopped;

        /// <summary>
        /// Called when Azure's Recognizing or Recognized events have been invoked.
        /// Passes the event on to <see cref="Recognized"/>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="final"></param>
        protected void OnSpeechEvent(SpeechRecognitionEventArgs e, bool final)
        {
            var language = AutoDetectSourceLanguageResult.FromResult(e.Result);

            string strEvent = final ? "Recognized" : "Recognizing";
            Logger.LogTrace($"{strEvent} ({language.Language}): {e.Result.Text}");

            if (string.IsNullOrWhiteSpace(e.Result.Text))
                //this happens occasionally
                return;

            var recognizedEvent = ServiceProvider.GetService(typeof(SpeechRecognizedEvent)) as SpeechRecognizedEvent;
            recognizedEvent.Preliminary = !final;
            recognizedEvent.ResultID = e.Result.OffsetInTicks.ToString();
            recognizedEvent.Text = e.Result.Text;
            recognizedEvent.Language = language.Language;

            Recognized?.Invoke(this, recognizedEvent);
        }

        #endregion

        #region "Audio"

        /// <summary>
        /// Constructs an <see cref="AudioConfig"/> from <see cref="Config"/>.
        /// Depending on the available services, this may either use the audio features built into the Speech SDK (such as <see cref="AudioConfig.FromDefaultMicrophoneInput"/>),
        /// or it may construct a <see cref="IStreamAudioSource"/> that accesses the requested <see cref="AudioDevice"/> with resampling and noise gates as required.
        /// </summary>
        /// <returns></returns>
        protected AudioConfig GetAudioConfig()
        {
            var streamSource = GetStreamAudioSource(Config.AudioSource);
            if (streamSource != null)
            {
                //use this stream source and convert to an Azure audio stream
                try
                {
                    var azureInput = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(
                        (uint)streamSource.Format.SampleRate,
                        (byte)streamSource.Format.BitsPerSample,
                        (byte)streamSource.Format.ChannelCount));

                    byte[] bufferOptional = null;
                    streamSource.DataAvailable += (s, e) =>
                    {
                        azureInput.Write(e.Buffer.GetArray(ref bufferOptional), e.Buffer.Count);
                    };
                    streamSource.Stopped += (s, e) =>
                    {
                        if (e.Cause == StreamAudioSourceStoppedCause.End)
                        {
                            //signal end-of-stream to Azure
                            azureInput.Close();
                        }
                    };

                    this.StreamAudioSource = streamSource;
                    return AudioConfig.FromStreamInput(azureInput);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error while creating an Azure AudioConfig from an IStreamAudioSource. Format: SampleRate={streamSource.Format.SampleRate}, BitsPerSample={streamSource.Format.BitsPerSample}, Channels={streamSource.Format.ChannelCount}");
                    streamSource.Dispose();
                }
            }

            this.StreamAudioSource = null;
            this.StreamAudioNoiseGate = null;

            //try and use the built-in audio engine
            if (Config.AudioSource is AudioDevice audioDevice)
            {
                if (audioDevice.UseDefaultAudioInputDevice)
                    return AudioConfig.FromDefaultMicrophoneInput();
            }

            return null;
        }

        /// <summary>
        /// See <see cref="GetAudioConfig"/>: constructs a <see cref="IStreamAudioSource"/> for the given audio source (usually an <see cref="AudioDevice"/>).
        /// Returns null if no <see cref="IStreamAudioSource"/> can be constructed from this source.
        /// </summary>
        /// <param name="configSource"></param>
        /// <returns></returns>
        protected IStreamAudioSource GetStreamAudioSource(IAudioSource configSource)
        {
            IStreamAudioSource streamSource = configSource as IStreamAudioSource;

            if (streamSource != null)
            {
                //take care to keep the original stream intact
                streamSource = new NonDisposingStreamAudioSource(streamSource);
            }
            else
            {
                //read from blob/device
                var provider = ServiceProvider.GetService(typeof(IStreamAudioSourceProvider)) as IStreamAudioSourceProvider;
                streamSource = provider?.GetStreamAudioSource(configSource);
            }
            if (streamSource == null)
            {
                Logger.LogError($"Cannot read from AudioDevice: could not construct an IStreamAudioSource");
                return null;
            }

            //add a noisegate to avoid unnecessary costs when there's no actual sound. probably best to do that before resampling => saves some processing power
            var noiseGateProvider = ServiceProvider.GetService(typeof(INoiseGateStreamAudioProcessProvider)) as INoiseGateStreamAudioProcessProvider;
            var noiseGate = noiseGateProvider?.GetNoiseGate(streamSource);
            if (noiseGate != null)
            {
                noiseGate.SetOptions(new NoiseGateStreamAudioProcessorOptions()
                {
                    VolumeThreshold = Config.VolumeThreshold,
                    Delay = TimeSpan.FromSeconds(5),
                    DelayStopDetection = TimeSpan.FromSeconds(2)
                });
                streamSource = noiseGate;
                StreamAudioNoiseGate = noiseGate;
            }
            else
            {
                Logger.LogWarning($"No noisegate available. Input format: {streamSource.Format}");
            }

            //check on the wave format
            //according to https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/audio-processing-overview#minimum-requirements-to-use-microsoft-audio-stack
            //-we need multiples of 16kHz
            //-"32-bit IEEE little endian float, 32-bit little endian signed int, 24-bit little endian signed int, 16-bit little endian signed int, or 8-bit signed int"
            //--(though the API doesn't seem to allow float formats)
            //additionally: more than one channel increases the API cost
            var sourceFormat = streamSource.Format;
            bool resampleRequired = sourceFormat.SampleRate % 16000 != 0 ||
                !sourceFormat.IntEncoded ||
                !sourceFormat.IntEncodingSigned ||
                !sourceFormat.ByteOrderLittleEndian ||
                sourceFormat.ChannelCount > 1;
            if (resampleRequired)
            {
                //get a resampler
                WaveStreamAudioFormat targetFormat = WaveStreamAudioFormat.GetIntFormat(
                    sampleRate: sourceFormat.SampleRate / 16000 * 16000,
                    bitsPerSample: Math.Min(sourceFormat.BitsPerSample / 8 * 8, 32),
                    channelCount: 1,
                    signed: true,
                    littleEndian: true
                );
                var provider = ServiceProvider.GetService(typeof(IStreamAudioResamplerProvider)) as IStreamAudioResamplerProvider;

                var resampler = provider?.GetResampler(streamSource, targetFormat);
                if (resampler != null)
                {
                    streamSource = resampler;
                }
                else
                {
                    //can't resample
                    Logger.LogError($"No resampler available. Input format: {sourceFormat}");
                    streamSource.Dispose();
                    return null;
                }
            }

            return streamSource;
        }

        /// <summary>
        /// Helper class for <see cref="GetStreamAudioSource(IAudioSource)"/>:
        /// wraps the <see cref="IStreamAudioSource"/> received via <see cref="SpeechRecognitionConfig"/>
        /// and does not dispose it when this object is disposed, thus keeping external audio sources intact.
        /// </summary>
        private class NonDisposingStreamAudioSource : IWrappedStreamAudioSource
        {
            public IStreamAudioSource WrappedAudioSource { get; private set; }

            public NonDisposingStreamAudioSource(IStreamAudioSource audioSource)
            {
                this.WrappedAudioSource = audioSource;
            }

            public void Dispose()
            {
                //do nothing
            }
        }

        #endregion

        #region "Credentials"

        /// <summary>
        /// Attempts to switch <see cref="Credentials"/> to the next available set of credentials.
        /// </summary>
        /// <returns>
        /// True: there was at least one more set of credentials available, thus <see cref="Credentials"/> points to a fresh set of credentials now.
        /// </returns>
        protected bool NextCredentials()
        {
            var list = Settings.Providers.Azure.Credentials;
            if (CredentialsIndex + 1 < list.Count)
            {
                ++CredentialsIndex;
                return true;
            }
            return false;
        }

        #endregion

        #region "Dispose"

        /// <summary>
        /// Disposes of the given disposables and clears the given list.
        /// </summary>
        /// <param name="disposables"></param>
        protected void Dispose(ref ICollection<IDisposable> disposables)
        {
            foreach (var disposable in disposables)
            {
                if (Object.ReferenceEquals(disposable, SpeechRecognizer))
                    SpeechRecognizer = null;
                disposable.Dispose();
            }
            disposables = new List<IDisposable>();
        }

        public void Dispose()
        {
            Dispose(ref Disposables);
            AudioConfig?.Dispose();
            StreamAudioSource?.Dispose();
        }

        #endregion
    }
}
