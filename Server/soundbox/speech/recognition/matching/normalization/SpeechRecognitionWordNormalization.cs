using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Soundbox.Speech.Recognition
{
    /// <summary>
    /// Utility methods to normalize words and sentences.
    /// Can be used to match spoken words against defined words => matching those words is possible even though the spoken may actually be slightly different.
    /// </summary>
    public static class SpeechRecognitionWordNormalization
    {
        #region "Normalization"

        private static readonly Regex NormalizationGermanPrefixRegex = new Regex("^(ge)");
        private static readonly Regex NormalizationGermanSuffixRegex = new Regex("(e[snmr]?|s|t)$");

        /// <summary>
        /// Normalizes the given words for matching.
        /// May or may not take the given language into account (e.g., in German language, this may remove suffixes from words to make them comparable when used in different contexts).
        /// </summary>
        /// <param name="words"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public static SpeechRecognitionNormalizedWords GetWordsNormalized(IEnumerable<string> words, string language)
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture;
            if (language != null)
                culture = System.Globalization.CultureInfo.GetCultureInfo(language);
            bool english = language != null && language.StartsWith("en");
            bool german = language != null && language.StartsWith("de");

            var normalized = new List<string>();
            foreach (var word in words)
            {
                var wordNormalized = word.ToLower(culture);

                if (english)
                {
                    wordNormalized = wordNormalized.Replace("'s", "s").Replace("'re", "r");
                }
                else if (german)
                {
                    wordNormalized = NormalizationGermanPrefixRegex.Replace(wordNormalized, "");
                    wordNormalized = NormalizationGermanSuffixRegex.Replace(wordNormalized, "");
                    wordNormalized = wordNormalized.Replace("ß", "ss");
                }

                normalized.Add(wordNormalized);
            }

            return new SpeechRecognitionNormalizedWords()
            {
                InputWords = words as IList<string> ?? words.ToList(),
                NormalizedWords = normalized
            };
        }

        #endregion

        #region "Sentence to words"

        private static readonly Regex PunctuationRegex = new Regex("[.\\-_?!]");

        /// <summary>
        /// Turns the given sentence into separate words and removes punctuations.
        /// </summary>
        /// <param name="sentence"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public static IList<string> ToWords(string sentence, string language)
        {
            string cleaned = PunctuationRegex.Replace(sentence, " ");
            return cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        #endregion
    }
}
