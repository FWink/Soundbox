import { ISpeechRecognizable } from './SpeechRecognizable';

export interface ISpeechRecognitionTestRecognizable extends ISpeechRecognizable {
    /**
     * Arbitrary ID that the client can use to identify the recognizable that has been detected.
     */
    id: string
}