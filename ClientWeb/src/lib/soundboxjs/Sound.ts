import { ISoundboxFile } from "./SoundboxFile";
import { ISoundMetaData } from "./SoundMetaData";
import { isDirectory } from "./SoundboxDirectory";
import { ISoundboxPlayable } from './SoundboxPlayable';

export interface ISound extends ISoundboxFile, ISoundboxPlayable {
    metaData: ISoundMetaData
}

export function isSound(file: ISoundboxFile): file is ISound {
    return !isDirectory(file);
}