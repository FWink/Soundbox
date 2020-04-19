import { ISoundboxFile } from "./SoundboxFile";
import { ISoundMetaData } from "./SoundMetaData";
import { isDirectory } from "./SoundboxDirectory";

export interface ISound extends ISoundboxFile {
    metaData: ISoundMetaData
}

export function isSound(file: ISoundboxFile): file is ISound {
    return !isDirectory(file);
}