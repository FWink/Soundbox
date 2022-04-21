using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Soundbox.Audio;
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

        //TODO azure: config
        protected string AzureRegion;
        protected string AzureSubscriptionKey;
        protected SpeechRecognitionConfig Config;

        /// <summary>
        /// True: recognition is currently active.
        /// </summary>
        protected bool IsRunning;
        protected SpeechRecognizer SpeechRecognizer;

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
        internal void SetConfig(SpeechRecognitionConfig config)
        {
            this.Config = config;
        }

        public async Task Start(SpeechRecognitionOptions options)
        {
            AudioConfig audioConfig = null;
            SpeechRecognizer recognizer = null;

            try
            {
                Logger.LogInformation("Starting speech recognition");

                var speechConfig = SpeechConfig.FromEndpoint(new Uri($"wss://{AzureRegion}.stt.speech.microsoft.com/speech/universal/v2"), AzureSubscriptionKey);
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

                if (Config.AudioSource is AudioDevice deviceAudioSource)
                {
                    if (deviceAudioSource.UseDefaultAudioDevice)
                        audioConfig = AudioConfig.FromDefaultMicrophoneInput();
                    else
                        audioConfig = AudioConfig.FromMicrophoneInput(deviceAudioSource.AudioDeviceName);
                }
                else
                {
                    throw new ArgumentException("AzureSpeechRecognitionService currently supports DeviceAudioSource only but was given: " + Config.AudioSource?.GetType().FullName);
                }

                recognizer = new SpeechRecognizer(speechConfig, languageConfig, audioConfig);

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
                    Logger.LogWarning($"Recognition stopped. reason={e.Reason}, erroCode={e.ErrorCode}, details={e.ErrorDetails}");

                    Stopped?.Invoke(this, new SpeechRecognitionStoppedEvent()
                    {
                        SpeechRecognizer = this,
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

                //start recognizing
                await recognizer.StartContinuousRecognitionAsync();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Could not start continuous recognition");

                recognizer?.Dispose();
                audioConfig?.Dispose();
                throw;
            }

            SpeechRecognizer = recognizer;
            IsRunning = true;

            Disposables.Add(recognizer);
            Disposables.Add(audioConfig);
        }

        public Task Stop()
        {
            return StopInt(true);
        }

        /// <summary>
        /// Implements <see cref="Stop"/>
        /// </summary>
        /// <param name="withEvent">
        /// False: <see cref="Stopped"/> is not triggered.
        /// </param>
        /// <returns></returns>
        protected Task StopInt(bool withEvent)
        {
            var recognizer = SpeechRecognizer;
            if (recognizer != null)
            {
                Logger.LogInformation("Stopping speech recognition");

                Dispose(Disposables);
                IsRunning = false;
                SpeechRecognizer = null;

                if (withEvent)
                {
                    Stopped?.Invoke(this, new SpeechRecognitionStoppedEvent()
                    {
                        SpeechRecognizer = this
                    });
                }
            }

            return Task.CompletedTask;
        }

        public async Task UpdateOptions(SpeechRecognitionOptions options)
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
                await Start(options);
            }
            catch (Exception e)
            {
                //could not restart
                Logger.LogError(e, "Could not restart recognition from UpdateOptions");

                Stopped?.Invoke(this, new SpeechRecognitionStoppedEvent()
                {
                    SpeechRecognizer = this,
                    Exception = e
                });
                return;
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

            var recognizedEvent = ServiceProvider.GetService(typeof(SpeechRecognizedEvent)) as SpeechRecognizedEvent;
            recognizedEvent.SpeechRecognizer = this;
            recognizedEvent.Preliminary = !final;
            recognizedEvent.ResultID = e.Result.OffsetInTicks.ToString();
            recognizedEvent.Text = e.Result.Text;
            recognizedEvent.Language = language.Language;

            Recognized?.Invoke(this, recognizedEvent);
        }

        #endregion

        #region "Dispose"

        /// <summary>
        /// Disposes of the given disposables and clears the given list.
        /// </summary>
        /// <param name="disposables"></param>
        protected void Dispose(ICollection<IDisposable> disposables)
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
            disposables.Clear();
        }

        public void Dispose()
        {
            Dispose(Disposables);
            Config.AudioSource.Dispose();
        }

        #endregion
    }
}
