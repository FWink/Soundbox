import { IPlaybackOptions } from "./PlaybackOptions";
import { ISound } from "./Sound";

export interface ISoundPlaybackRequest {
    sounds: {
        sound: ISound,
        options?: IPlaybackOptions
    }[];
}