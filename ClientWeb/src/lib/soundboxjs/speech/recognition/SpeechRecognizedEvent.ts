/**
 *
 * Generated when the speech detection successfully recognized and transcribed spoken words.<br/>
 * Results here may be {@link #preliminary}, as the speech recognizer might not work word-for-word, but on entire sentences and paragraphs.
 * Thus, events here may represent preliminary results for a part of a sentence, but the speech recognizer might settle on a better translation once the spoken paragraph is complete.
 * For that purpose, all related events share a same {@link #resultID}: events with the same {@link #resultID} have the same start point in the audio recording,
 * but newer events may include additional words as they are being spoken. A chain of events with the same <see cref="ResultID"/> might look like this:<ol>
 * <li>Lorem</li>
 * <li>Lorem ipsum</li>
 * <li>Lorem ipsum dohor [sic]</li>
 * <li>Lorem ipsum dolor sit [previous word got changed as the speech recognizer settled on a different word]</li>
 * <li>Lorem ipsum dolor sit amet</li>
 * <li>Lorem ipsum dolor sit amet [final result]</li>
 * </ol>
 * However, some speech recognizers may indeed work in a word-for-word mode (<see cref="WordResult"/>),
 * then you'd get every word or short bursts of words delivered separately.*/
export interface SpeechRecognizedEvent {
    /**
     * Not unique per event, but per transcribed group of words.
     */
    resultID: string,
    /**
     * True: the transcribed text is not final and words that have been recognized already may change with further events for this <see cref="ResultID"/>.
     */
    preliminary: boolean,
    /**
     * True: the speech recognizer works in a word-for-word mode as opposed to transcribing entire sentences or paragraphs in one go.
     */
    wordResult: boolean,
    /**
     * The entire transcribed text.
     */
    text: string,
    /**
     * Optional: detected language.
     * Should expect two-letter and five-letter codes here.
     */
    language?: string
}