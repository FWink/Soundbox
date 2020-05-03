import { ISoundboxDirectory, isDirectory } from './SoundboxDirectory';
import { ISoundboxFile } from './SoundboxFile';
import { ISound, isSound } from './Sound';

/**
 * Contains a tree of sound files and directories.
 * Provides methods to query the tree, for example to search for nodes with a certain ID.
 * */
export class SoundsDatabase {
    public root: ISoundboxDirectory;

    public constructor(root: ISoundboxDirectory) {
        if (root)
            this.root = this.copy(root);
    }

    /**
     * Returns a deep copy of the given directory.
     * @param directory
     */
    protected copy(directory: ISoundboxDirectory) {

        //copy the directory itself
        const copy: ISoundboxDirectory = {
            id: directory.id,
            iconUrl: directory.iconUrl,
            name: directory.name,
            tags: directory.tags,
            watermark: directory.watermark,
            children: [],
            parentDirectory: null
        };

        //deep copy the cildren directories
        for (const child of directory.children) {

            let childCopy: ISoundboxFile;

            if (isDirectory(child)) {
                childCopy = this.copy(child);
                childCopy.parentDirectory = copy;
            }
            else if (isSound(child)) {
                let childSound: ISound = {
                    id: child.id,
                    iconUrl: child.iconUrl,
                    name: child.name,
                    tags: child.tags,
                    metaData: child.metaData,
                    parentDirectory: copy
                };

                childCopy = childSound;
            }
            else {
                continue;
            }

            copy.children.push(childCopy);
        }

        return copy;
    }

    /**
     * Finds a node with the given ID.
     * @param id
     */
    public findById(id: string): ISoundboxFile {
        return this.findByIdInDirectory(id, this.root);
    }

    /**
     * Helper for {@link findById(string)}: recursively searches through the given directory and its descendants.
     * @param id
     * @param file
     */
    protected findByIdInDirectory(id: string, file: ISoundboxFile): ISoundboxFile {
        if (file.id == id)
            return file;

        if (isDirectory(file)) {

            for (let child of file.children) {
                let found = this.findByIdInDirectory(id, child);
                if (found)
                    return found;
            }
        }

        //not found in directory
        return null;
    }
}