import { ISoundboxDirectory } from "./SoundboxDirectory";
import { ISoundboxFile } from "./SoundboxFile";

export interface ISoundboxFileChangeEvent {
    file: ISoundboxFile,
    event: SoundboxFileChangeEventType,
    /**
     * The root {@link ISoundboxDirectory#watermarks} before the change occurred.
     * This can be used by clients to check if they missed any event.<br/>
     * Note: the current watermark can be retrieved from {@link #file};
     * either directly because it is a {@link #ISoundboxDirectory} or from its {@link #ISoundboxFile#parentDirectory}.
     **/
    previousWatermark: string
}

export interface ISoundboxFileMoveEvent extends ISoundboxFileChangeEvent {
    /**
     * Directory the file has been moved from.
     **/
    fromDirectory: ISoundboxDirectory
}

export enum SoundboxFileChangeEventType {
    ADDED,
    MODIFIED,
    DELETED,
    MOVED
}