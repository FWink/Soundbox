/**
 * Contains voice-activation settings for a single sound.
 * */
export interface ISoundboxVoiceActivation {
    /**
     * Words or sentences that trigger this voice-activated command.
     * */
    speechTriggers: string[],

    /**
     * Special words or phrases included in {@link #speechTriggers} that are hard to detect:
     * you wouldn't usually expect a speech recognition software to be able to detect these words.
     * By specifying them here, we can help the speech recognition and tell it to look specifically for these words.
     * */
    speechPhrases: string[]
}