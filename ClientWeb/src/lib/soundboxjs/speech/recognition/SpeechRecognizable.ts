/**
 * Represents a voice-activated "command": one or more text triggers (words or even entire sentences)
 * that can be matched against the result of a speech recognition operation {@link SpeechRecognizedEvent}.
 */
export interface ISpeechRecognizable {
    /**
     * Words or sentences that trigger this voice-activated command.
     */
    speechTriggers: string[],

    /**
     * Probability that this command is executed when one of {@link speechTriggers} has been detected. Values (0;1]<br/>
     * This is a bit of placeholder: at some point (when the speech recognition algorithm is a bit more sophisticated) this should be replaced by a "weight".
     * Then, when there are multiple commands that match the same trigger, the executed command is selected randomly from but the weight decides the probability for each command (default weight = 1).<br/>
     * As it is now, the Soundbox selects the first match and then decides based on this probability, whether to use a command or to keep searching for other matches.
     * The big difference here is, that a simple probability does not take the probability/weight values of other commands into account.
     * Thus, if the user isn't careful, a matching trigger might not execute any action, when there's no action with a probability of 100%.
     */
    speechProbability?: number
}