using Microsoft.VisualStudio.TestTools.UnitTesting;
using Soundbox.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Text;

namespace Soundbox.Test
{
    [TestClass]
    public class SpeechRecognitionMatcherTest
    {
        protected IServiceProvider ServiceProvider;
        protected SpeechRecognitionMatcher Matcher;

        public SpeechRecognitionMatcherTest()
        {
            ServiceProvider = new TestServiceProvider();
            Matcher = ServiceProvider.GetService(typeof(SpeechRecognitionMatcher)) as SpeechRecognitionMatcher;
        }

        [TestMethod]
        public void LongTextStackOverflowTest()
        {
            string word = "test ";
            string text = "";

            var recognizables = new List<ISpeechRecognizable>();
            for (int i = 0; i < 100; ++i)
            {
                recognizables.Add(new TestSpeechRecognizable("none"));
            }

            for (int i = 0; i < 500; ++i)
            {
                text += word;
                var speechEvent = ServiceProvider.GetService(typeof(SpeechRecognizedEvent)) as SpeechRecognizedEvent;
                speechEvent.ResultID = "resultID";
                speechEvent.Preliminary = true;
                speechEvent.WordResult = false;
                speechEvent.Language = "en-US";
                speechEvent.Text = text;

                Matcher.Match(speechEvent, recognizables);
            }
        }

        protected class TestSpeechRecognizable : ISpeechRecognizable
        {
            public TestSpeechRecognizable(params string[] triggers) : this((ICollection<string>)triggers) { }

            public TestSpeechRecognizable(ICollection<string> triggers)
            {
                this.SpeechTriggers = triggers;
            }

            public ICollection<string> SpeechTriggers { get; }
        }
    }
}
