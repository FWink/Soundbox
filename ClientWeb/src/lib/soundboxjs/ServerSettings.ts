export interface IServerSettingsRequest {
    /**
     * The volume passed to {@link Soundbox} is mapped to a scale of 0-maxVolume effectively limiting the Soundbox's volume to a certain level.
     */
    maxVolume?: number;
}

export interface IServerSettings {
    /**
     * The volume passed to {@link Soundbox} is mapped to a scale of 0-maxVolume effectively limiting the Soundbox's volume to a certain level.
     */
    maxVolume: number;
}