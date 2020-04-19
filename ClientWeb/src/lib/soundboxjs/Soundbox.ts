import * as signalR from '@microsoft/signalr';
import { Observable, Subject, ReplaySubject } from 'rxjs';
import { IServerSettings, IServerSettingsRequest } from './ServerSettings';
import { ISound, isSound } from './Sound';
import { ISoundboxDirectory, isDirectory } from './SoundboxDirectory';
import { ISoundPlaybackRequest } from './SoundPlaybackRequest';
import { ISoundboxFileBase } from './SoundboxFile';

/**
 * Represents a Soundbox server with a two-way realtime communication channel (via SignalR).
 * Provides a high-level promise-based RPC mechanism to call server-side methods such as "edit sound"
 * and subscription-based event callbacks for events such as "volume changed".
 */
export class Soundbox {

    /**
     * The base URL of the server. We can derive the SignalR endpoint and the REST endpoints from this.
     */
    protected readonly baseUrl: string;

    protected connection: signalR.HubConnection;

    /**
     * 
     * @param baseUrl The Soundbox server's base URL. Omit to have it detected from the current browser location.
     */
    public constructor(baseUrl?: string) {
        if (!baseUrl) {
            baseUrl = "";
        }
        this.baseUrl = baseUrl;
    }

    /**
     * Starts the Soundbox handler and connects to the server.
     * Promise resolves once the server connection has been established.
     * At that point any server queries can be performed, though the following operations are performed automatically:
     * * fetch volume (see {@link volume}).
     * * fetch settings (see {@link settings})
     */
    public start(): Promise<void> {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(this.baseUrl + "/api/v1/ws")
            .build();

        return connection.start()
            .then(() => {
                this.connection = connection;

                //setup listeners once
                this.connection.on("OnVolumeChanged", (volume: number) => {
                    this.onVolumeChanged(volume);
                });
                this.connection.on("OnSettingMaxVolumeChanged", (maxVolume: number) => {
                    this.onMaxVolumeChanged(maxVolume);
                });
            })
            //further setup for each reconnect
            .then(() => {
                return this.onConnected();
            });
    }

    /**
     * Called when we (re-)connect to the Soundbox server. Takes care of some setup such as requesting the current volume.
     */
    protected onConnected(): Promise<void> {
        return this.connection.invoke("GetVolume")
            .then((volume: number) => {
                this.onVolumeChanged(volume);
            })

            .then(() => {
                return this.connection.invoke("GetSettingMaxVolume");
            })
            .then((maxVolume: number) => {
                this.onMaxVolumeChanged(maxVolume);
            })
    }

    //#region volume

    /**
     * Alias of {@link volume}. Used to emit changes in the volume.
     */
    protected readonly volumeSubject: Subject<number> = new ReplaySubject<number>(1);

    /**
     * Updated whenever the Soundbox volume is changed on the server. That includes when the volume is changed from this Soundbox instance via {@link setVolume}.
     */
    public readonly volume: Observable<number> = this.volumeSubject;

    /**
     * The Soundbox's current volume level (i.e. the last volume inserted into {@link volume})
     */
    protected currentVolume: number;

    /**
     * Changes the Soundbox's current output volume (global).
     * @param volume
     */
    public setVolume(volume: number): Promise<void> {
        return this.connection.invoke("SetVolume", volume);
    }

    /**
     * Returns the Soundbox's current global volume level.
     */
    public getVolume(): number {
        return this.currentVolume;
    }

    /**
     * Called whenever a new volume value has been received from the server.
     * @param volume
     */
    protected onVolumeChanged(volume: number) {
        this.currentVolume = volume;
        this.volumeSubject.next(volume);
    }

    //#endregion

    //#region Sounds

    /**
     * Returns all avilable sounds from the server in no particular order.
     * */
    public getSounds(): Promise<ISound[]> {
        //TODO temporary
        return this.connection.invoke("GetSounds", null, true)
            .then((directories: ISoundboxDirectory[]) => {
                const root = directories[0];
                return this.getSoundsFromDirectoryLocal(root);
            });
    }

    /**
     * Recursive helper function to get all the sounds in this directory and its descendants.
     * @param directory
     */
    private getSoundsFromDirectoryLocal(directory: ISoundboxDirectory): ISound[] {
        const sounds: ISound[] = [];

        for (const file of directory.children) {

            if (isDirectory(file)) {
                sounds.push.apply(sounds, this.getSoundsFromDirectoryLocal(file));
            }
            else if(isSound(file)) {
                sounds.push(file);
            }
        }

        return sounds;
    }

    //#endregion

    //#region Playback

    /**
     * Requests to play one or multiple sounds.
     * @param request
     * @returns Promise that resolves if the server accepts the request and started handling it.
     */
    public play(request: ISoundPlaybackRequest): Promise<void>;
    public play(request: ISound): Promise<void>;
    public play(requestOrSound: ISoundPlaybackRequest | ISound): Promise<void> {

        if (!this.isRequest(requestOrSound)) {
            requestOrSound = {
                sounds: [
                    {
                        sound: requestOrSound
                    }
                ]
            };
        }

        let request: ISoundPlaybackRequest = requestOrSound;
        request = this.flattenRequest(request);

        return this.connection.invoke("Play", request);
    }

    /**
     * Helper function for {@link play}
     * @param requestOrSound
     */
    protected isRequest(requestOrSound: ISoundPlaybackRequest | ISound): requestOrSound is ISoundPlaybackRequest {
        return (requestOrSound as ISoundPlaybackRequest).sounds !== undefined;
    }

    /**
     * Flattens the request's sounds by applying {@link flattenFile}.
     * @param request
     */
    protected flattenRequest(request: ISoundPlaybackRequest): ISoundPlaybackRequest {
        for (let i = 0; i < request.sounds.length; ++i) {
            request.sounds[i].sound = this.flattenFile(request.sounds[i].sound);
        }

        return request;
    }

    /**
     * Immediately stops any playback.
     * */
    public stop(): Promise<void> {
        return this.connection.invoke("Stop");
    }

    //#endregion

    //#region Settings

    /**
     * Alias of {@link settings}. Used to emit changes in the settings.
     */
    protected readonly settingsSubject: Subject<IServerSettings> = new ReplaySubject<IServerSettings>(1);

    /**
     * Updated whenever the Soundbox settings are changed on the server. That includes when the settings are changed from this Soundbox instance via {@link setSettings}.
     */
    public readonly settings: Observable<IServerSettings> = this.settingsSubject;

    /**
     * The Soundbox's current settings.
     */
    protected currentSettings: IServerSettings;

    /**
     * Changes the Soundbox's current global settings
     * @param settings
     */
    public setSettings(settings: IServerSettingsRequest): Promise<any> {

        const promises: Promise<any>[] = [];

        if (settings.maxVolume !== undefined) {
            //set max volume
            promises.push(this.connection.invoke("SetSettingMaxVolume", settings.maxVolume));
        }

        return Promise.all(promises);
    }

    /**
     * Returns the Soundbox's current global settings.
     */
    public getSettings(): IServerSettings {
        return this.currentSettings;
    }

    /**
     * Called whenever a new max volume setting has been received from the server.
     * @param volume
     */
    protected onMaxVolumeChanged(volume: number) {
        if (this.currentSettings == null) {
            this.currentSettings = {
                maxVolume: volume
            };
        }
        else {
            this.currentSettings.maxVolume = volume;
        }

        this.settingsSubject.next(this.currentSettings);
    }

    //#endregion

    //#region Utilities

    /**
     * Flattens the given file into its most basic core ({@link ISoundboxFileBase}) which is sufficient to send to the server for several calls.
     * @param file
     */
    protected flattenFile<T extends ISoundboxFileBase>(file: T): T {
        //hack but we know what we're doing here
        return {
            id: file.id
        } as T;
    }

    //#endregion
}