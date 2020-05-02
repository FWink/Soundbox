import { Component, OnInit } from '@angular/core';
import { Soundbox } from '../lib/soundboxjs/Soundbox';
import { ISound } from '../lib/soundboxjs/Sound';
import { IUploadStatus } from '../lib/soundboxjs/UploadStatus';

@Component({
    templateUrl: 'SoundboxMainPage.html'
})
export class SoundboxMainPage implements OnInit {

    soundbox: Soundbox;
    sounds: ISound[] = [];

    pitch: number = 100;
    uploadName: string;
    uploadFiles: File[];
    uploadStatus: IUploadStatus;

    constructor(soundbox: Soundbox) {
        this.soundbox = soundbox;
    }

    ngOnInit(): void {
        this.soundbox.start()
            .then(() => {
                return this.soundbox.getSounds();
            })
            .then(sounds => {
                this.sounds = sounds;
            });
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

    upload() {
        if (this.uploadFiles && this.uploadFiles.length > 0) {
            this.uploadStatus = this.soundbox.upload(this.uploadFiles[0], {
                name: this.uploadName
            });
        }
    }

    uploadAbort() {
        if (this.uploadStatus) {
            this.uploadStatus.abort();
        }
    }
}