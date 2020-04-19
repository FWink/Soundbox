export interface ISoundUpload {
    /**
     * Display name
     * */
    name: string;

    /**
     * List of tags/categories to easily find similar files. E.g. "Star Wars", "Funny", "Meme"...
     * */
    tags?: string[];
}

/**
 * Full sound parameter that the server accepts.
 * */
export interface ISoundUploadFull extends ISoundUpload {
    fileName: string;
}