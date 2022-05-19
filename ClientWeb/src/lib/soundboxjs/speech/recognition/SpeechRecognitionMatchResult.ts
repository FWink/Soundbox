import { ISpeechRecognitionTestRecognizable } from './SpeechRecognitionTestRecognizable';

export interface SpeechRecognitionMatchResult {
    recognizable: ISpeechRecognitionTestRecognizable,
    /**
     * True: the recognizable matches the spoken text exactly or close enough.
     */
    success: boolean,
    /**
     * On {@link #success}: the spoken words that were detected as a match for the {@link #recognizable}.
     * Note that these are in fact the recognized *spoken* words as opposed to the defined {@link ISpeechRecognizable#speechTriggers} of the recognizable.
     */
    wordsSpokenMatched: string[]
}