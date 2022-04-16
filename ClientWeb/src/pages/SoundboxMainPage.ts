import { Component, OnInit } from '@angular/core';
import { Soundbox } from '../lib/soundboxjs/Soundbox';
import { ISound } from '../lib/soundboxjs/Sound';
import { IUploadStatus, IUploadProgress } from '../lib/soundboxjs/UploadStatus';
import { stat } from 'fs';

@Component({
    templateUrl: 'SoundboxMainPage.html',
    styleUrls: ['SoundboxMainPage.scss']
})
export class SoundboxMainPage implements OnInit {

    soundbox: Soundbox;

    pitch: number = 100;

    newSoundsPending: NewSound[] = [];
    uploadStatusCurrent: IUploadStatus;
    uploadProgressTotal: IUploadProgress;
    uploadsInProgress: number = 0;

    constructor(soundbox: Soundbox) {
        this.soundbox = soundbox;
    }

    ngOnInit(): void {
        this.soundbox.start();
    }

    play(sound: ISound) {
        this.soundbox.play({
            sounds: [
                {
                    sound: sound,
                    options: {
                        speedPitch: this.pitch / 100
                    }
                }
            ]
        });
    }

    volumeIncrement() {
        this.soundbox.setVolume(this.soundbox.getVolume() + 5);
    }

    volumeDecrement() {
        this.soundbox.setVolume(this.soundbox.getVolume() - 5);
    }

    /**
     * Called when the user selects some files to upload them.
     * Allows the user to edit each sound before uploading them.
     * @param files
     */
    onFilesSelected(files: File[]) {
        this.newSoundsPending = [];
        this.uploadStatusCurrent = null;
        this.uploadProgressTotal = null;

        for (let file of files) {
            this.newSoundsPending.push(new NewSound(this.soundbox, file));
        }
    }

    /**
     * Uploads all files in {@link #newSoundsPending}
     * */
    uploadAll() {
        let queue = [...this.newSoundsPending];

        let next = (i: number) => {
            //set progress
            this.uploadProgressTotal = {
                done: i,
                total: queue.length
            };

            if (i >= queue.length) {
                //done
                --this.uploadsInProgress;
                return;
            }

            let status = this.uploadSound(queue[i]);
            status.done
                .then(() => this.removeFinishedUpload(queue[i]))
                .finally(() => next(i + 1));
        }

        ++this.uploadsInProgress;
        next(0);
    }

    /**
     * Uploads one single sound to the soundbox and removes it from {@link #newSoundsPending} on success.
     * @param sound
     */
    upload(sound: NewSound) {
        let status = this.uploadSound(sound);

        let statusSub = status.progress.subscribe(progress => this.uploadProgressTotal = progress);
        status.done.then(() => {
            this.removeFinishedUpload(sound);
        })
            .finally(() => statusSub.unsubscribe());
    }

    protected uploadSound(sound: NewSound): IUploadStatus {

        let status = this.soundbox.upload(sound.file, {
            name: sound.name,
            voiceActivation: {
                speechTriggers: sound.getSpeechTriggers(),
                speechPhrases: sound.getSpeechPhrases()
            }
        });

        this.uploadStatusCurrent = status;
        ++this.uploadsInProgress;
        status.done.finally(() => --this.uploadsInProgress);

        return status;
    }

    /**
     * Removes a sound from {@link #newSoundsPending}
     * @param sound
     */
    protected removeFinishedUpload(sound: NewSound) {
        let deleteIndex = this.newSoundsPending.indexOf(sound);
        if (deleteIndex >= 0)
            this.newSoundsPending.splice(deleteIndex, 1);
    }
}

/**
 * Represents one new sound that the user is currently setting up (entering a name and additional information).
 * */
class NewSound {
    public readonly soundbox: Soundbox;
    public readonly file: File;

    public get fileName(): string {
        return this.file.name;
    }

    public name: string;

    public speechTriggers: string;

    public speechPhrases: string;

    public constructor(soundbox: Soundbox, file: File) {
        this.soundbox = soundbox;
        this.file = file;
        this.name = NewSound.getNameFromFileName(this.fileName);
        this.speechTriggers = this.name;
    }

    /**
     * Turns the given file name into a human-readable default name for the uploaded sound.
     * @param fileName
     */
    public static getNameFromFileName(fileName: string): string {
        //remove file type
        let name = fileName.replace(/\.[^.]+$/, "");
        //remove some whitespace replacements
        name = name.replace(/[\-_;]/g, " ");
        //remove multiple white spaces
        name = name.replace(/\s{2,}/g, " ");

        return name;
    }

    /**
     * Returns the entered speech triggers as a list.
     * */
    public getSpeechTriggers(): string[] {
        if (!this.speechTriggers)
            return [];
        return this.speechTriggers.split(";").map(str => str.trim()).filter(str => str.length > 0);
    }

    /**
     * Returns the entered speech phrases as a list.
     * */
    public getSpeechPhrases(): string[] {
        if (!this.speechPhrases)
            return [];
        return this.speechPhrases.split(";").map(str => str.trim()).filter(str => str.length > 0);
    }
}