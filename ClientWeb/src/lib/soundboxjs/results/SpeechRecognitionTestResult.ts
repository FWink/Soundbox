import { IServerResult } from './ServerResult';
import { SpeechRecognizedEvent } from '../speech/recognition/SpeechRecognizedEvent';
import { SpeechRecognitionMatchResult } from '../speech/recognition/SpeechRecognitionMatchResult';

export interface SpeechRecognitionTestResult extends IServerResult {
    /**
     * Contains transcribed words that were spoken in the uploaded clip.
     */
    speechEvent: SpeechRecognizedEvent,
    /**
     * Contains which words (if any) matched the input speech triggers. Included only if there was in fact a match for this event.
     */
    matchResult: SpeechRecognitionMatchResult,
    /**
     * True: this is the last event for the recorded clip. The clip has been processed completely.
     */
    end: boolean
}