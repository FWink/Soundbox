import { ISoundboxVoiceActivation } from './SoundboxVoiceActivation';

/**
 * Represents something the soundbox can play: a {@link ISound} or a macro (consisting of {@link ISound}s).
 * This is usually also a {@link ISoundboxFile}.
 * Optionally voice-activated.
 * */
export interface ISoundboxPlayable {
    /**
     * Enables voice-activation/speech-recognition on this playable: saying a word or sentence in voice chat
     * causes this playable to be played automatically by the server.
     * */
    voiceActivation?: ISoundboxVoiceActivation
}