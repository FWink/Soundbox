import { ISoundboxFile } from "./SoundboxFile";

export interface ISoundboxDirectory extends ISoundboxFile {
    children: ISoundboxFile[];
    /**
     * Unique watermark that gets updated whenever a change in {@link Children} occurs (recursive)
     * */
    watermark: string;
}

export function isDirectory(file: ISoundboxFile): file is ISoundboxDirectory {
    return (file as ISoundboxDirectory).children !== undefined ||
        (file as ISoundboxDirectory).watermark !== undefined;
}