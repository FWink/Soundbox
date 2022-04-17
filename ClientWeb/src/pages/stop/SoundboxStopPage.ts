import { OnInit, Component } from '@angular/core';
import { Soundbox } from '../../lib/soundboxjs/Soundbox';

@Component({
    templateUrl: "SoundboxStopPage.html"
})
export class SoundboxStopPage implements OnInit {

    constructor(protected readonly soundbox: Soundbox) {}

    ngOnInit(): void {
        this.soundbox.start();
    }

    /**
     * Stops any currently playing sound.
     * */
    stop() {
        this.soundbox.stop();
    }
}