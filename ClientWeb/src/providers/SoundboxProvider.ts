import { FactoryProvider } from "@angular/core";
import { Soundbox } from '../lib/soundboxjs/Soundbox';
import { environment } from '../environments/environment';

class SoundboxProvider implements FactoryProvider {

    /**
     * Singleton
     */
    protected static soundbox: Soundbox;

    provide = Soundbox;
    useFactory = () => {
        if (SoundboxProvider.soundbox == null) {
            SoundboxProvider.soundbox = new Soundbox(environment.soundboxEndpoint);
            SoundboxProvider.soundbox.start();
        }

        return SoundboxProvider.soundbox;
    }
    deps?: any[];

}

export let soundboxProvider = new SoundboxProvider();