/**
 * The most basic file properties that are required when talking to the server.
 */
export interface ISoundboxFileBase {
    id: string
}

export interface ISoundboxFile extends ISoundboxFileBase {
    /**
     * Display name
     * */
    name: string;

    /**
     * Absolute URL for the file's icon/image.
     * */
    iconUrl: string;

    /**
     * List of tags/categories to easily find similar files. E.g. "Star Wars", "Funny", "Meme"...
     * */
    tags: string[];
}

export abstract class SoundboxFile implements ISoundboxFile {
    id: string;
    name: string;
    iconUrl: string;
    tags: string[];
}