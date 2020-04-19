import { IServerResult } from './ServerResult';
import { ISoundboxFile } from '../SoundboxFile';
import { ISoundboxDirectory } from '../SoundboxDirectory';

export interface IFileResult extends IServerResult {
    file: ISoundboxFile,
    /**
     * The root watermark before the operation started.
     * */
    previousWatermark: string,
    /**
     * For move operations: the directory where {@link file} came from.
     * */
    fromDirectory: ISoundboxDirectory
}