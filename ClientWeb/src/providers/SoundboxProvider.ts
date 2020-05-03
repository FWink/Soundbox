import { FactoryProvider } from "@angular/core";
import { Soundbox } from '../lib/soundboxjs/Soundbox';
import { environment } from '../environments/environment';
import { DefaultStorageProvider } from '../lib/soundboxjs/DefaultStorageProvider';

class SoundboxProvider implements FactoryProvider {

    /**
     * Singleton
     */
    protected soundbox: Soundbox;

    provide = Soundbox;
    useFactory = () => {
        if (this.soundbox == null) {
            this.soundbox = new Soundbox(new DefaultStorageProvider("Soundbox."), environment.soundboxEndpoint);
            this.soundbox.start();
        }

        return this.soundbox;
    }
    deps?: any[];

}

export let soundboxProvider = new SoundboxProvider();