import { Component, OnInit } from "@angular/core";
import { ISound } from "../../lib/soundboxjs/Sound";
import { Soundbox } from "../../lib/soundboxjs/Soundbox";

@Component({
    templateUrl: "SoundboxRandomPage.html"
})
export class SoundboxRandomPage implements OnInit {

    protected sounds: ISound[];
    protected lastSound: ISound;

    constructor(protected soundbox: Soundbox) { }

    ngOnInit(): void {
        this.soundbox.sounds.subscribe(sounds => this.sounds = sounds);
        this.soundbox.start();
    }

    public playRandom() {
        let sound = this.sounds[Math.floor(Math.random() * this.sounds.length)];
        this.lastSound = sound;

        this.soundbox.play(sound);
    }

    public playLast() {
        if (this.lastSound) {
            this.soundbox.play(this.lastSound);
        }
    }
}