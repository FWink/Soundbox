import { ISpeechRecognizable } from './speech/recognition/SpeechRecognizable';

/**
 * Contains voice-activation settings for a single sound.
 * */
export interface ISoundboxVoiceActivation extends ISpeechRecognizable {
    /**
     * Special words or phrases included in {@link #speechTriggers} that are hard to detect:
     * you wouldn't usually expect a speech recognition software to be able to detect these words.
     * By specifying them here, we can help the speech recognition and tell it to look specifically for these words.
     * */
    speechPhrases: string[]
}