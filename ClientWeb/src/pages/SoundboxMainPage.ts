import { Component, OnInit } from '@angular/core';
import { Soundbox } from '../lib/soundboxjs/Soundbox';
import { ISound } from '../lib/soundboxjs/Sound';
import { environment } from '../environments/environment';

@Component({
    templateUrl: 'SoundboxMainPage.html'
})
export class SoundboxMainPage implements OnInit {

    protected soundbox: Soundbox;
    public sounds: ISound[] = [];

    constructor() {
        this.soundbox = new Soundbox(environment.soundboxEndpoint);
    }

    ngOnInit(): void {
        this.soundbox.start().then(() => {
            this.soundbox.getSounds().then(sounds => {
                this.sounds = sounds;
            });
        });
    }

    play(sound: ISound) {
        this.soundbox.play(sound);
    }
}