import * as signalR from '@microsoft/signalr';
import { Observable, Subject, ReplaySubject, BehaviorSubject, Subscription } from 'rxjs';
import { IServerSettings, IServerSettingsRequest } from './ServerSettings';
import { ISound, isSound } from './Sound';
import { ISoundboxDirectory, isDirectory } from './SoundboxDirectory';
import { ISoundPlaybackRequest } from './SoundPlaybackRequest';
import { ISoundboxFileBase, ISoundboxFile } from './SoundboxFile';
import { IPlayingNow } from './PlayingNow';
import { ISoundUpload, ISoundUploadFull } from './Upload';
import { IFileResult } from './results/FileResult';
import { SoundboxError } from './errors/SoundboxError';
import { ResultStatusCode, fromHttp } from './results/ResultStatusCode';
import { IUploadStatus, IUploadProgress } from './UploadStatus';
import { StorageProvider } from './StorageProvider';
import { SoundsDatabase } from './SoundsDatabase';

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

    /**
     * Persistent storage where we keep the list of sounds between sessions.
     * */
    protected readonly storage: StorageProvider;

    protected connection: signalR.HubConnection;
    /**
     * True: {@link start} has been called successfully and we are ready to go.
     */
    protected connected: boolean = false;
    /**
     * True: {@link start} is currently working.
     */
    protected connecting: boolean = false;
    /**
     * Used in {@link start} to update the caller should {@link connecting} be true when the method is called (only one connection attempt may be underway at once).
     */
    protected connectingSubject: Subject<void>;

    /**
     * 
     * @param storage Persistent storage where the soundbox keeps a list of available sounds to prevent loading it from the server on each new session, thus decreasing the startup time.
     * @param baseUrl The Soundbox server's base URL. Omit to have it detected from the current browser location.
     */
    public constructor(storage: StorageProvider, baseUrl?: string) {
        if (!baseUrl) {
            baseUrl = "";
        }
        this.baseUrl = baseUrl;
        this.storage = storage;
    }

    /**
     * Starts the Soundbox handler and connects to the server or ensures that we are connected to the server for repeated calls.
     * Promise resolves once the server connection has been established.
     * At that point any server queries can be performed, though the following operations are performed automatically:
     * * fetch volume (see {@link volume}).
     * * fetch settings (see {@link settings})
     * To not miss any updates you should attach your subscribers before calling this method.
     */
    public start(): Promise<void> {
        if (this.connected) {
            //already connected
            return Promise.resolve();
        }
        else if (this.connecting) {
            return this.connectingSubject.toPromise();
        }

        this.connection?.stop();

        this.connecting = true;
        this.connectingSubject = new Subject<void>();

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
                this.connection.on("OnSoundsPlayingChanged", (playingNow: IPlayingNow[]) => {
                    this.onPlaybackChanged(playingNow);
                });
            })
            //further setup for each reconnect
            .then(() => {
                return this.onConnected();
            })
            .then(() => {
                this.connected = true;
                this.connectingSubject.next();
                this.connectingSubject.complete();
            })
            .finally(() => {
                this.connecting = false;
            });
    }

    /**
     * Closes the connection to the soundbox server.
     **/
    public dispose() {
        if (this.connecting) {
            //wait until we are connected
            this.start()
                .finally(() => {
                    this.dispose();
                });
            return;
        }

        this.connection?.stop();
        this.connecting = false;
        this.connected = false;
    }

    /**
     * Called when we (re-)connect to the Soundbox server. Takes care of some setup such as requesting the current volume.
     */
    protected onConnected(): Promise<any> {

        const promises: Promise<any>[] = [];

        promises.push(this.loadSounds());
        promises.push(
            this.connection.invoke("GetVolume")
                .then((volume: number) => {
                    this.onVolumeChanged(volume);
                }));
        promises.push(
            this.connection.invoke("GetSettingMaxVolume")
                .then((maxVolume: number) => {
                    this.onMaxVolumeChanged(maxVolume);
                }));
        promises.push(
            this.connection.invoke("GetSoundsPlayingNow")
                .then((playingNow: IPlayingNow[]) => {
                    this.onPlaybackChanged(playingNow);
                }));

        return Promise.all(promises);
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

    protected static readonly STORAGE_KEY_SOUNDS_TREE = "soundstree";

    protected currentSoundsTree: ISoundboxDirectory;

    protected soundsTreeFetch: Subject<ISoundboxDirectory> = new ReplaySubject<ISoundboxDirectory>(1);

    /**
     * ID that uniquely identifies a query process of the server's list of sound.
     * If the server's sound list changes while we're currently fetching it (i.e. an OnFileEvent arrives while we're recursively calling GetSounds)
     * then we need to restart that query. We'll increment this ID which causes the active task to terminate and start a new query.
     */
    protected soundsQueryId: number = 0;

    /**
     * Wrapper for server GetSounds RPC
     * @param directory
     * @param recursive
     */
    protected serverGetSounds(directory: ISoundboxDirectory, recursive: boolean): Promise<ISoundboxDirectory[]> {
        return this.connection.invoke("GetSounds", directory, recursive);
    }

    /**
     * Loads the entire tree of sounds from the server on startup. If we have sounds stored in {@link storage} then we do a quick watermark comparison with the server's root directory.
     * In best case we detect that our list is up-to-date and do not need to load anything further from the server.
     * Otherwise the server's files and directories are loaded as needed.
     * */
    protected loadSounds(): Promise<ISoundboxDirectory> {

        let serverPromise = this.serverGetSounds(null, false);
        let storagePromise = this.storage.get(Soundbox.STORAGE_KEY_SOUNDS_TREE);

        let fromStorage: ISoundboxDirectory;
        let fromServer: ISoundboxDirectory;

        return serverPromise
            .then((serverRootWrapper: ISoundboxDirectory[]) => {
                fromServer = serverRootWrapper[0];
                return storagePromise;
            })
            .then((storageRootDirectory: ISoundboxDirectory) => {
                fromStorage = storageRootDirectory;
            })
            .then(() => {

                if (fromStorage) {
                    //perform a quick watermark check
                    if (fromStorage.watermark == fromServer.watermark) {
                        //we are up-to-date
                        return fromStorage;
                    }
                    //start fetching at the root level
                    return this.loadDirectory(fromServer, new SoundsDatabase(fromStorage));
                }
                else {
                    //nothing stored, fetch the entire file tree
                    return this.loadDirectory(fromServer, null);
                }
            })
            .then((root: ISoundboxDirectory) => {
                this.currentSoundsTree = root;
                this.soundsTreeFetch.next(root);

                return root;
            });
    }

    /**
     * Compares the given directory's content with what is stored in the given database.
     * If there are differences then the directory is updated in the database and its descendants are fetched from the server as required.
     * If the database is null then we never performed a full fetch in the first place.
     * This is only ever called if we never performed a full fetch OR we're performing an update from the server and the given directory is new or its watermark does not match our stored watermark.
     * @param directory Directory fetched from the server via GetSounds. Its children have not been fetched yet
     * @param database
     */
    protected loadDirectory(directory: ISoundboxDirectory, database: SoundsDatabase): Promise<ISoundboxDirectory> {

        //load children
        return this.serverGetSounds(this.minimizeFile(directory), false)
            .then((children: ISoundboxFile[]) => {

                let childrenLoaded: Promise<ISoundboxFile>[] = [];

                for (let child of children) {
                    if (isDirectory(child)) {
                        if (database) {
                            let directoryStored = database.findById(child.id) as ISoundboxDirectory;
                            if (!directoryStored || child.watermark != directoryStored.watermark) {
                                //not stored or we detected a difference => load directory
                                childrenLoaded.push(this.loadDirectory(child, database));
                            }
                            else {
                                //no difference, continue using what we have stored
                                childrenLoaded.push(Promise.resolve(directoryStored));
                            }
                        }
                        else {
                            //first full fetch, nothing is stored => load directory
                            childrenLoaded.push(this.loadDirectory(child, database));
                        }
                    }
                    else {
                        //use the sound file from the server response
                        childrenLoaded.push(Promise.resolve(child));
                    }
                }

                return Promise.all(childrenLoaded);
            })
            .then((children: ISoundboxFile[]) => {
                //at this point we have updated all descendants
                directory.children = children;
                //make sure they all point to the right directory
                for (let child of children) {
                    child.parentDirectory = directory;
                }

                return directory;
            });
    }

    /**
     * Returns all avilable sounds from the server in no particular order.
     * */
    public getSounds(): Promise<ISound[]> {
        //TODO temporary
        let resolved = false;

        return new Promise<ISound[]>((resolve, reject) => {
            let soundsSubscription: Subscription;
            soundsSubscription = this.soundsTreeFetch.subscribe(root => {
                //we get into trouble here if the subscription immediately fires a value
                if(soundsSubscription)
                    soundsSubscription.unsubscribe();

                if (resolved)
                    return;
                resolved = true;

                resolve(this.getSoundsFromDirectoryLocal(root));
            });
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

        let request: ISoundPlaybackRequest;

        if (!this.isRequest(requestOrSound)) {
            request = {
                sounds: [
                    {
                        sound: requestOrSound
                    }
                ]
            };
        }
        else {
            request = requestOrSound;
        }

        request = this.minimizeRequest(request);

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
     * Minimizes and flattens the request's sounds by applying {@link minimizeFile}.
     * @param request
     */
    protected minimizeRequest(request: ISoundPlaybackRequest): ISoundPlaybackRequest {
        for (let i = 0; i < request.sounds.length; ++i) {
            request.sounds[i].sound = this.minimizeFile(request.sounds[i].sound);
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

    //#region PlaybackMonitor

    /**
     * Alias of {@link playingNow}. Used to emit changes in the current playback state.
     */
    protected readonly playingNowSubject: Subject<IPlayingNow[]> = new ReplaySubject<IPlayingNow[]>(1);

    /**
     * Updated whenever the Soundbox's playback state changes on the server. That includes when this Soundbox instance changes the playback via {@link play} or {@link stop}.
     */
    public readonly playingNow: Observable<IPlayingNow[]> = this.playingNowSubject;

    /**
     * The Soundbox's current playback.
     */
    protected currentPlayingNow: IPlayingNow[];

    /**
     * Returns the Soundbox's current playback.
     */
    public getPlayingNow(): IPlayingNow[] {
        return this.currentPlayingNow;
    }

    /**
     * Called whenever the Soundbox's playback state changed.
     * @param playingNow
     */
    protected onPlaybackChanged(playingNow: IPlayingNow[]) {
        this.currentPlayingNow = playingNow;
        this.playingNowSubject.next(playingNow);
    }

    //#endregion

    //#region Upload

    /**
     * Creates a proper formatted display name for a sound with the given file name.
     * This is used in {@link upload} when no display name has been supplied.
     * @param fileName
     */
    protected makeSoundName(fileName: string): string {
        let displayName: string;
		//replace _ by spaces ("surprise_motherf" -> "surprise motherf"
        displayName = fileName.replace("_", " ");

        //remove file type
        displayName = displayName.replace(/\.[^.]*$/, "");

        //TODO we could capitalize words

        return displayName;
    }

    /**
     * Uploads a new sound into the Soundbox.
     * @param file
     * @param sound
     * @param directory
     */
    public upload(file: File, sound: ISoundUpload, directory?: ISoundboxDirectory): IUploadStatus {

        //finished true: upload is done or aborted or an error occurred or...
        let finished: boolean = false;
        let aborted: boolean = false;

        //prepare the result object
        let status: IUploadStatus;

        //progress output
        const progressTotal: number = file.size;
        const progress: Subject<IUploadProgress> = new BehaviorSubject<IUploadProgress>({
            done: 0,
            total: progressTotal
        });

        //prepare the HTTP request and an abort function
        let request: XMLHttpRequest;
        let abort: () => void = () => {
            if (finished)
                return;

            aborted = true;
            if (request) {
                request.abort();
            }
        };

        //start the upload asynchronously
        const done = new Promise<ISound>((resolve, reject) => {

            //add the file name to the request
            const soundFull: ISoundUploadFull = {
                name: sound.name,
                tags: sound.tags,
                fileName: file.name
            };
            sound = soundFull;

            if (!sound.name) {
                sound.name = this.makeSoundName(soundFull.fileName);
            }

            //prepare a function that we can call on abort
            let onabort = () => {
                if (finished)
                    return;

                finished = true;
                reject(new SoundboxError({
                    code: ResultStatusCode.UPLOAD_ABORTED
                }));
            };

            //build URL from parameters. body will be the file only (binary)
            let url = this.baseUrl + "/api/v1/rest/sound?sound=" + encodeURIComponent(JSON.stringify(sound));
            if (directory) {
                url += "&directory=" + encodeURIComponent(JSON.stringify(this.minimizeFile(directory)));
            }

            const reader = new FileReader();

            request = new XMLHttpRequest();
            request.open("POST", url);
            //set headers for binary file transfer
            request.setRequestHeader("Content-Type", "application/octet-stream");
            request.setRequestHeader("Content-Length", file.size.toString());

            //prepare to send a result to the client
            request.onreadystatechange = () => {
                if (finished)
                    return;

                if (request.readyState == XMLHttpRequest.DONE) {
                    finished = true;

                    if (request.status == 200) {
                        const result: IFileResult = JSON.parse(request.response);

                        if (result.success) {
                            //make 100% sure we get a nice progress output
                            progress.next({
                                done: progressTotal,
                                total: progressTotal
                            });

                            //and finally return the uploaded sound
                            resolve(result.file as ISound);
                        }
                        else {
                            reject(new SoundboxError(result.status));
                        }
                    }
                    else {
                        reject(new SoundboxError({
                            code: fromHttp(request.status)
                        }));
                    }
                }
            };
            request.onerror = request.onabort = request.ontimeout =
                request.upload.onerror = request.upload.onabort = request.upload.ontimeout = () => {

                if (finished)
                    return;

                if (aborted) {
                    onabort();
                    return;
                }
                finished = true;

                reject(new SoundboxError({
                    code: ResultStatusCode.CONNECTION_ERROR
                }));
            };
            request.upload.onprogress = ev => {
                if (finished)
                    return;

                progress.next({
                    done: ev.loaded,
                    total: progressTotal
                });
            };

            //send when file has been read
            reader.onload = evt => {
                if (aborted) {
                    onabort();
                    return;
                }

                request.send(evt.target.result);
            };
            //start reading file
            reader.readAsArrayBuffer(file);
        });

        status = {
            done: done,
            progress: progress,
            abort: abort
        };

        return status;
    }

    //#endregion

    //#region Utilities

    /**
     * Flattens the given file into its most basic core ({@link ISoundboxFileBase}) which is sufficient to send to the server for several calls.
     * @param file
     */
    protected minimizeFile<T extends ISoundboxFileBase>(file: T): T {
        //hack but we know what we're doing here
        return {
            id: file.id
        } as T;
    }

    //#endregion
}