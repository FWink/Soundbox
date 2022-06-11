using Microsoft.VisualStudio.TestTools.UnitTesting;
using Soundbox.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Linq;
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
            string word = "test";
            string text = "";
            bool modeWordResult = GetTestModeWordResult();

            var recognizables = new List<ISpeechRecognizable>();
            for (int i = 0; i < 100; ++i)
            {
                recognizables.Add(new TestSpeechRecognizable("none"));
            }

            for (int i = 0; i < 500; ++i)
            {
                if (modeWordResult)
                    text = word;
                else
                    text += word + " ";
                var speechEvent = GetSpeechEvent(text);

                Matcher.Match(speechEvent, recognizables);
            }
        }

        [TestMethod]
        [DataRow("test", "test")]
        [DataRow("test", "test word")]
        [DataRow("test", "test word word")]
        [DataRow("test", "word test")]
        [DataRow("test", "word test word")]
        public void SingleWordSimpleTest(string trigger, string spoken)
        {
            var recognizable = new TestSpeechRecognizable(trigger);
            var speechEvent = GetSpeechEvent(spoken);

            var result = Matcher.Match(speechEvent, recognizable);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.WordsSpokenMatched.Count);
            Assert.AreSame(recognizable, result.Recognizable);

            result = Matcher.Match(speechEvent, recognizable);
            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        [DataRow("test", "test", 1)]
        [DataRow("test", "test test", 2)]
        [DataRow("test", "test word test", 2)]
        [DataRow("test", "test word test word", 2)]
        [DataRow("test", "test word test word test", 3)]
        [DataRow("test", "word test word test", 2)]
        public void SingleWordMultiTest(string trigger, string spoken, int countExpected)
        {
            var recognizable = new TestSpeechRecognizable(trigger);
            var speechEvent = GetSpeechEvent(spoken);

            SpeechRecognitionMatchResult result;

            while (countExpected-- > 0)
            {
                result = Matcher.Match(speechEvent, recognizable);

                Assert.IsTrue(result.Success);
                Assert.AreEqual(1, result.WordsSpokenMatched.Count);
                Assert.AreSame(recognizable, result.Recognizable);
            }

            result = Matcher.Match(speechEvent, recognizable);
            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        [DataRow("test", "test")]
        [DataRow("test", "test word")]
        [DataRow("test", "test word word")]
        [DataRow("test", "word test")]
        [DataRow("test", "word test word")]
        public void SingleWordSimpleChainTest(string trigger, string spoken)
        {
            var recognizable = new TestSpeechRecognizable(trigger);
            var events = GetSpeechEventChainFromText(spoken);

            SpeechRecognitionMatchResult result;

            int countMatches = 0;
            foreach (var speechEvent in events)
            {
                result = Matcher.Match(speechEvent, recognizable);

                if (result.Success)
                {
                    ++countMatches;
                    Assert.AreEqual(1, result.WordsSpokenMatched.Count);
                    Assert.AreSame(recognizable, result.Recognizable);
                }
            }

            Assert.AreEqual(1, countMatches);

            result = Matcher.Match(events.Last(), recognizable);
            Assert.IsFalse(result.Success);
        }

        #region "Helpers"

        /// <summary>
        /// Constructs a plain <see cref="SpeechRecognizedEvent"/> from the given parameters.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="language"></param>
        /// <param name="resultID"></param>
        /// <param name="preliminary"></param>
        /// <param name="wordResult"></param>
        /// <returns></returns>
        protected virtual SpeechRecognizedEvent GetSpeechEvent(string text, string language = "en-US", string resultID = "resultID", bool preliminary = true, bool? wordResult = null)
        {
            var speechEvent = ServiceProvider.GetService(typeof(SpeechRecognizedEvent)) as SpeechRecognizedEvent;
            speechEvent.ResultID = resultID;
            speechEvent.WordResult = wordResult ?? GetTestModeWordResult();
            if (speechEvent.WordResult)
                speechEvent.Preliminary = false;
            else
                speechEvent.Preliminary = preliminary;
            speechEvent.Language = language;
            speechEvent.Text = text;

            return speechEvent;
        }

        /// <summary>
        /// Returns whether <see cref="GetSpeechEvent(string, string, string, bool, bool?)"/> should return events with mode <see cref="SpeechRecognizedEvent.WordResult"/> by default.
        /// </summary>
        /// <returns></returns>
        protected virtual bool GetTestModeWordResult()
        {
            return false;
        }

        /// <summary>
        /// Turns the given text into words and then turns them into a list of related speech events.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        protected virtual IList<SpeechRecognizedEvent> GetSpeechEventChainFromText(string text, string language = "en-US")
        {
            return GetSpeechEventChainFromWords(language, text.Split(" "));
        }

        /// <summary>
        /// Turns the given list of words into a list of related speech events.
        /// </summary>
        /// <param name="language"></param>
        /// <param name="words"></param>
        /// <returns></returns>
        protected virtual IList<SpeechRecognizedEvent> GetSpeechEventChainFromWords(string language, params string[] words)
        {
            var events = new List<SpeechRecognizedEvent>();

            string text = "";
            foreach (string word in words)
            {
                if (text.Length > 0)
                    text += " ";
                text += word;
                
                events.Add(GetSpeechEvent(text, language: language));
            }

            events.Last().Preliminary = false;

            return events;
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

        #endregion
    }
}
