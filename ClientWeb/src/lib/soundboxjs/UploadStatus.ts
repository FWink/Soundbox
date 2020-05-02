import { ISound } from './Sound';
import { Observable } from 'rxjs';

export interface IUploadStatus {
    /**
     * Resolves to {@link ISound} on success. Throws a {@link SoundboxError} on any error including user abort.
     * */
    done: Promise<ISound>,
    /**
     * Upload progress.
     * Publishes the progress in bytes that have been uploaded/are remaining. That allows the client to
     * easily add the progress of several uploads in order to get a proper weighted progress
     * over all uploads.
     * */
    progress: Observable<IUploadProgress>,
    /**
     * Aborts the uplaod process. {@link progress} will stop outputting values and {@link done} will
     * throw an error with status {@link ResultStatusCode#UPLOAD_ABORTED}
     * */
    abort(): void
}

export interface IUploadProgress {
    /**
     * Progress that has been completed
     * */
    done: number,
    /**
     * Total progress until completion
     * */
    total: number
}