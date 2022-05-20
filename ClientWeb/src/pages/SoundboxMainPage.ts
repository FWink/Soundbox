import { Component, OnInit, NgZone } from '@angular/core';
import { Soundbox } from '../lib/soundboxjs/Soundbox';
import { ISound } from '../lib/soundboxjs/Sound';
import { IUploadStatus, IUploadProgress } from '../lib/soundboxjs/UploadStatus';
import { DomSanitizer } from '@angular/platform-browser';
// @ts-ignore
import OpusMediaRecorder from 'opus-media-recorder/OpusMediaRecorder.umd.js';

@Component({
    templateUrl: 'SoundboxMainPage.html',
    styleUrls: ['SoundboxMainPage.scss']
})
export class SoundboxMainPage implements OnInit {

    sanitizer: DomSanitizer;

    soundbox: Soundbox;

    pitch: number = 100;

    sounds: ISound[];

    newSoundsPending: NewSound[] = [];
    uploadStatusCurrent: IUploadStatus;
    uploadProgressTotal: IUploadProgress;
    uploadsInProgress: number = 0;

    editSounds: EditSound[] = [];

    constructor(soundbox: Soundbox, sanitizer: DomSanitizer, protected zone: NgZone) {
        this.soundbox = soundbox;
        this.sanitizer = sanitizer;
    }

    ngOnInit(): void {
        this.soundbox.sounds.subscribe(newSoundList => {
            this.sounds = newSoundList;

            //check on the sounds we're editing right now
            let ids = newSoundList.map(sound => sound.id);
            this.editSounds = this.editSounds.filter(editSound => ids.includes(editSound.sound.id));
        });

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

    //#region Upload

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
            this.newSoundsPending.push(new NewSound(file));
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

    //#endregion

    //#region Context menu

    contextMenuActive: boolean;
    contextMenuSound: ISound;
    contextMenuX: number;
    contextMenuY: number;

    /**
     * Opens a context menu on the given event's location, allowing the user to edit and delete the given sound.
     * @param sound
     * @param e
     */
    showSoundContextMenu(sound: ISound, e: MouseEvent) {

        this.contextMenuActive = true;
        this.contextMenuSound = sound;
        this.contextMenuX = e.pageX;
        this.contextMenuY = e.pageY;

        e.stopPropagation();
        e.preventDefault();
    }

    /**
     * Hides any active sound context menu.
     **/
    hideSoundContextMenu() {
        this.contextMenuActive = false;
    }

    //#endregion

    //#region Delete

    /**
     * Deletes the given sound from the server. Our list of sounds will be updated automatically.
     * @param sound
     */
    delete(sound: ISound) {
        this.soundbox.delete(sound);
    }

    //#endregion

    //#region Edit

    /**
     * Starts editing this sound: shows a UI where the sound's name, voice activation etc can be modified.
     * @param sound
     */
    startEdit(sound: ISound) {
        if (!this.editSounds.find(editSound => editSound.sound.id === sound.id))
            this.editSounds.push(new EditSound(sound));
    }

    /**
     * Calls {@link startEdit} on all our sounds, thus creating an editable table of all sounds.
     */
    startEditAll() {
        for (let sound of this.sounds) {
            this.startEdit(sound);
        }
    }

    /**
     * Stops editing the given sound.
     * @param sound
     */
    stopEdit(sound: EditSound) {
        for (let i = 0; i < this.editSounds.length; ++i) {
            if (this.editSounds[i] == sound) {
                this.editSounds.splice(i, 1);
                return;
            }
        }
    }

    /**
     * Sends the sound's modified inputs to the server and on success, stops editing this sound.
     * The list of sounds is updated automatically.
     * @param sound
     */
    edit(sound: EditSound) {
        let editSound: ISound = {
            ...sound.sound
        };
        editSound.name = sound.name;
        editSound.voiceActivation = {
            speechTriggers: sound.getSpeechTriggers(),
            speechPhrases: sound.getSpeechPhrases()
        };

        this.soundbox.edit(editSound).then(() => {
            this.stopEdit(sound);
        });
    }

    //#endregion

    //#region Speech recognition test

    speechRecognitionRecordingPending: boolean;
    speechRecognitionRecordingRunning: boolean;
    speechRecognitionRecorder: MediaRecorder;
    speechRecognitionRecordingChunks: Blob[];

    speechRecognitionRecordedAudio: Blob;
    speechRecognitionRecordedAudioSrc: string;


    /**
     * Starts recording audio with the user's microphone and asks for permission first as required.
     */
    recordSpeechRecognitionTestAudio() {
        this.speechRecognitionRecordingPending = true;

        navigator.mediaDevices.getUserMedia({
            audio: true
        })
        .then(stream => {
            let mimeTypeOggOpus = "audio/ogg; codecs=opus";

            let recorderOptions: MediaRecorderOptions = {
                mimeType: mimeTypeOggOpus
            };

            let recorder: MediaRecorder;
            if (recorderOptions.mimeType == mimeTypeOggOpus && !MediaRecorder.isTypeSupported(mimeTypeOggOpus)) {
                //use the OpusMediaRecorder lib
                let opusBasePath = "scripts/opus-media-recorder/";
                let workerOptions = {
                    encoderWorkerFactory: () => new Worker(opusBasePath + "encoderWorker.umd.js"),
                    OggOpusEncoderWasmPath: 'OggOpusEncoder.wasm',
                    WebMOpusEncoderWasmPath: 'WebMOpusEncoder.wasm'
                };

                recorder = new OpusMediaRecorder(stream, recorderOptions, workerOptions);
            }
            else {
                recorder = new MediaRecorder(stream, recorderOptions);
            }

            recorder.ondataavailable = event => {
                this.speechRecognitionRecordingChunks.push(event.data);
            };

            recorder.onstop = () => {
                this.zone.run(() => {
                    this.speechRecognitionRecordingRunning = false;
                    this.speechRecognitionRecordedAudio = new Blob(this.speechRecognitionRecordingChunks, this.speechRecognitionRecordingChunks[0]);
                    this.speechRecognitionRecordedAudioSrc = URL.createObjectURL(this.speechRecognitionRecordedAudio);

                    for (let track of stream.getTracks()) {
                        track.stop();
                    }
                });
            };

            this.speechRecognitionRecordingRunning = true;
            this.speechRecognitionRecorder = recorder;
            this.speechRecognitionRecordingChunks = [];
            this.speechRecognitionRecordedAudio = null;
            this.speechRecognitionRecordedAudioSrc = null;
            recorder.start();
        })
        .finally(() => {
            this.speechRecognitionRecordingPending = false;
        });
    }

    /**
     * Stops the test recording started via {@link #recordSpeechRecognitionTestAudio}
     */
    stopSpeechRecognitionTestAudioRecording() {
        this.speechRecognitionRecorder.stop();
    }

    uploadSpeechRecognitionTestAudio() {
        this.soundbox.testSpeechRecognition(this.speechRecognitionRecordedAudio).subscribe(console.log);
    }

    //#endregion
}

/**
 * A sound that is currently being uploaded or edited.
 * */
class EditSound {
    public readonly sound?: ISound;

    public name: string;

    public speechTriggers: string;

    public speechPhrases: string;

    public constructor(sound?: ISound) {
        this.sound = sound;
        if (sound) {
            this.name = sound.name;
            if (sound.voiceActivation) {
                this.speechTriggers = sound.voiceActivation.speechTriggers.join(";");
                this.speechPhrases = sound.voiceActivation.speechPhrases.join(";");
            }
        }
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

/**
 * Represents one new sound that the user is currently setting up (entering a name and additional information).
 * */
class NewSound extends EditSound {
    public readonly file: File;

    public get fileName(): string {
        return this.file.name;
    }

    public constructor(file: File) {
        super(null);
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
}