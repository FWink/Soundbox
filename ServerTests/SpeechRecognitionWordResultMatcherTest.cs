using Microsoft.VisualStudio.TestTools.UnitTesting;
using Soundbox.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Text;

namespace Soundbox.Test
{
    /// <summary>
    /// Runs the same tests as <see cref="SpeechRecognitionMatcherTest"/> but uses events in mode <see cref="SpeechRecognizedEvent.WordResult"/>.
    /// </summary>
    [TestClass]
    public class SpeechRecognitionWordResultMatcherTest : SpeechRecognitionMatcherTest
    {
        protected override bool GetTestModeWordResult()
        {
            return true;
        }

        protected override IList<SpeechRecognizedEvent> GetSpeechEventChainFromWords(string language, params string[] words)
        {
            var events = new List<SpeechRecognizedEvent>();

            foreach (string word in words)
            {
                events.Add(GetSpeechEvent(word, language: language, wordResult: true, preliminary: false));
            }

            return events;
        }
    }
}
