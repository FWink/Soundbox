﻿using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
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

        public AzureSpeechRecognitionService() { }

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
                var speechConfig = SpeechConfig.FromEndpoint(new Uri($"{AzureRegion}.stt.speech.microsoft.com/speech/universal/v2"), AzureSubscriptionKey);
                speechConfig.SetProfanity(ProfanityOption.Raw);
                speechConfig.SetProperty(PropertyId.SpeechServiceConnection_ContinuousLanguageIdPriority, "Latency");

                var languageConfig = AutoDetectSourceLanguageConfig.FromLanguages(options.Languages.ToArray());

                if (Config.AudioSource is DeviceAudioSource deviceAudioSource)
                {
                    if (deviceAudioSource.UseDefaultAudioInputDevice)
                        audioConfig = AudioConfig.FromDefaultMicrophoneInput();
                    else
                        audioConfig = AudioConfig.FromMicrophoneInput(deviceAudioSource.AudioInputDeviceName);
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
            catch
            {
                recognizer.Dispose();
                audioConfig.Dispose();
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

            try
            {
                await StopInt(false);
                await Start(options);
            }
            catch (Exception e)
            {
                //could not restart
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
            Recognized?.Invoke(this, new SpeechRecognizedEvent()
            {
                SpeechRecognizer = this,
                Preliminary = !final,
                ResultID = e.Result.OffsetInTicks.ToString(),
                Text = e.Result.Text
            });
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
