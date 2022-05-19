/**
 * Represents a voice-activated "command": one or more text triggers (words or even entire sentences)
 * that can be matched against the result of a speech recognition operation {@link SpeechRecognizedEvent}.
 */
export interface ISpeechRecognizable {
    /**
     * Words or sentences that trigger this voice-activated command.
     */
    speechTriggers: string[]
}