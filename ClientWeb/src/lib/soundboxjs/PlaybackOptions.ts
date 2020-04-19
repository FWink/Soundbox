export interface IPlaybackOptions {
    /**
     * Static volume on a 1-100 scale (relative to the overall sound volume).
     * */
    volume?: number;
    /**
     * Playback speed which affects the pitch as well: doubling the speed increases the pitch by an octave.
     * */
    speedPitch?: number;
    /**
     * When the sound is being played as part of a chain of playbacks this denotes a break/delay between the end of the current and the start of the next sound.
     * May be negative.
     * */
    chainDelayMs?: number;
    /**
     * Whether to clip the current sound for a negative {@link ChainDelayMs}. False: sounds may overlap.
     * */
    chainDelayClip?: boolean;
}