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
import { ISoundboxFileChangeEvent, ISoundboxFileMoveEvent, SoundboxFileChangeEventType } from './SoundboxFileChangeEvent';
import { SoundboxConnectionState } from './SoundboxConnectionState';
import { IServerResult } from './results/ServerResult';
import { ISpeechRecognitionTestRecognizable } from './speech/recognition/SpeechRecognitionTestRecognizable';
import { ISoundboxPlayable } from './SoundboxPlayable';
import { SpeechRecognitionTestResult } from './results/SpeechRecognitionTestResult';

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

    //#region connection

    protected connection: signalR.HubConnection;

    /**
     * True: {@link start} has been called successfully and we are ready to go.
     */
    protected get connected(): boolean {
        return this.connectionStateCurrent == SoundboxConnectionState.Connected;
    }
    /**
     * True: {@link start} is currently working.
     */
    protected get connecting(): boolean {
        let state = this.connectionStateCurrent;
        return state == SoundboxConnectionState.Connecting || state == SoundboxConnectionState.Reconnecting;
    }

    /**
     * Used in {@link start} to update the caller should {@link connecting} be true when the method is called (only one connection attempt may be underway at once).
     */
    protected connectingPromise: Promise<any>;

    /**
     * Our internal connection state (from {@link connectionStateSubject}).
     * @param state
     */
    protected set connectionStateCurrent(state: SoundboxConnectionState) {
        this.connectionStateSubject.next(state);
    }

    protected get connectionStateCurrent(): SoundboxConnectionState {
        return this.connectionStateSubject.getValue();
    }

    /**
     * Alias of {@link connectionState}. Used to emit changes in the connection state.
     */
    protected readonly connectionStateSubject: BehaviorSubject<SoundboxConnectionState> = new BehaviorSubject<SoundboxConnectionState>(SoundboxConnectionState.Initial);

    /**
     * Updated when the soundbox's connection state changed.
     */
    public readonly connectionState: Observable<SoundboxConnectionState> = this.connectionStateSubject;

    /**
     * Starts the Soundbox handler and connects to the server or ensures that we are connected to the server for repeated calls.
     * Promise resolves once the server connection has been established.
     * At that point any server queries can be performed, though the following operations are performed automatically:
     * * fetch volume (see {@link volume}).
     * * fetch settings (see {@link settings})
     * To not miss any updates you should attach your subscribers before calling this method.
     */
    public start(): Promise<any> {
        if (this.connected) {
            //already connected
            return Promise.resolve();
        }
        else if (this.connecting) {
            return this.connectingPromise;
        }

        this.connection?.stop();

        this.connectionStateCurrent = SoundboxConnectionState.Connecting;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(this.baseUrl + "/api/v1/ws")
            .withAutomaticReconnect()
            .build();

        this.connectingPromise = connection.start()
            .then(() => {
                this.connection = connection;

                //set up listeners once
                this.connection.on("OnVolumeChanged", (volume: number) => {
                    this.onVolumeChanged(volume);
                });
                this.connection.on("OnSettingMaxVolumeChanged", (maxVolume: number) => {
                    this.onMaxVolumeChanged(maxVolume);
                });
                this.connection.on("OnSoundsPlayingChanged", (playingNow: IPlayingNow[]) => {
                    this.onPlaybackChanged(playingNow);
                });
                this.connection.on("OnFileEvent", (event: ISoundboxFileChangeEvent) => {
                    this.onFileChangeEvent(event);
                });
            })
            //further setup for each reconnect
            .then(() => {
                return this.onConnected()
                    .catch(error => {
                        this.onConnectFetchError(connection, error);
                        throw error;
                    });
            })
            .then(() => {
                this.connectionStateCurrent = SoundboxConnectionState.Connected;
            })
            .catch(error => {
                //automatic reconnect doesn't work here
                this.connectionStateCurrent = SoundboxConnectionState.Disconnected;
                throw error;
            })
            .finally(() => {
                this.connectingPromise = null;
            });

        //set up the reconnected/disconnected handling
        connection.onreconnecting(() => {
            this.connectionStateCurrent = SoundboxConnectionState.Reconnecting;

            //wait for the next connection state change
            this.connectingPromise = new Promise((resolve, reject) => {
                let first = true;
                let subscription = this.connectionState.subscribe(state => {
                    if (first) {
                        //is a BehaviorSubject. we'll always get one event right away
                        first = false;
                        return;
                    }

                    this.connectingPromise = null;
                    if (state == SoundboxConnectionState.Connected)
                        resolve();
                    else
                        reject("Could not reconnect");

                    if (subscription)
                        subscription.unsubscribe();
                });
            });
        })
        connection.onreconnected(() => {
            this.onConnected()
                .then(() => {
                    this.connectionStateCurrent = SoundboxConnectionState.Connected;
                })
                .catch(error => {
                    this.onConnectFetchError(connection, error);
                });
        });
        connection.onclose(() => {
            this.connectionStateCurrent = SoundboxConnectionState.Disconnected;
        });

        return this.connectingPromise;
    }

    /**
     * Called from {@link #start} after we've connected successfully but after {@link #onConnected} threw an error.
     * This closes the connection and transitions into {@link SoundboxConnectionState#Disconnected}.
     * @param connection
     */
    private onConnectFetchError(connection: signalR.HubConnection, error?: any) {
        connection?.stop();
        this.connectionStateCurrent = SoundboxConnectionState.Disconnected;
        console.error("Unrecoverable error after connecting", error);
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
        this.connectionStateCurrent = SoundboxConnectionState.Disconnected;
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

    //#endregion

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

    /**
     * Promise that resolves when updating our local list of sounds has completed.
     * Used as a queue-like mechanism: soundsFetch = soundsFetch.then(...)
     */
    protected soundsFetch: Promise<any> = Promise.resolve();

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
        let storagePromise: Promise<ISoundboxDirectory>;
        if (this.currentSoundsTree)
            //already loaded this earlier
            storagePromise = Promise.resolve(this.currentSoundsTree);
        else
            //load from storage
            storagePromise = this.storage.get(Soundbox.STORAGE_KEY_SOUNDS_TREE);

        let fromStorage: ISoundboxDirectory;
        let fromServer: ISoundboxDirectory;

        let result = serverPromise
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

                return root;
            });

        this.soundsFetch = result
            .then(() => this.onSoundListUpdated());

        return result;
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
     * Returns our local copy of the given server file.
     * @param file
     */
    protected getFile<T extends ISoundboxFile>(file: T): T {
        return this.findFile(this.currentSoundsTree, file.id) as T;
    }

    /**
     * Returns our local copy of the file with the given id.
     * @param directory
     * @param id
     */
    protected findFile(directory: ISoundboxDirectory, id: string): ISoundboxFile {
        if (directory.id == id)
            return directory;

        for (let child of directory.children) {
            if (isDirectory(child)) {
                //search recursively
                let found = this.findFile(child, id);
                if (found)
                    return found;

                continue;
            }

            if (child.id == id)
                return child;
        }

        return null;
    }

    /**
     * Recursively updates the watermark in the given and all its ancestor directories.
     * @param file If it is a file, then its parent is used instead.
     * @param watermark
     */
    protected updateWatermark(file: ISoundboxFile, watermark: string) {

        let directory: ISoundboxDirectory;
        if (isDirectory(file))
            directory = file;
        else
            directory = file.parentDirectory;

        while (directory) {
            directory.watermark = watermark;
            directory = directory.parentDirectory;
        }
    }

    //#region events
    //TODO this is all a placeholder implementation. eventually this will be re-structured to utilize a directory-based approach

    /**
     * Alias of {@link sounds}. Used to emit changes in the list of sounds.
     */
    protected readonly soundsSubject: Subject<ISound[]> = new ReplaySubject<ISound[]>(1);

    /**
     * Updated whenever the Soundbox volume is changed on the server. That includes when the volume is changed from this Soundbox instance via {@link setVolume}.
     */
    public readonly sounds: Observable<ISound[]> = this.soundsSubject;

    /**
     * Called when the server notifies us about an update to the list of sounds.
     * @param event
     */
    protected onFileChangeEvent(event: ISoundboxFileChangeEvent) {
        //event.type is sent as string
        event.event = SoundboxFileChangeEventType[event.event as any as string as keyof typeof SoundboxFileChangeEventType];

        this.soundsFetch = this.soundsFetch
            .then(() => {
                let file = event.file;

                let watermarkNow: string;
                if (isDirectory(file)) {
                    watermarkNow = file.watermark;
                }
                else {
                    watermarkNow = file.parentDirectory.watermark;
                }

                if (this.currentSoundsTree.watermark == watermarkNow) {
                    //we seemed to have fetched this updated already. nothing to do
                    return;
                }
                if (this.currentSoundsTree.watermark != event.previousWatermark) {
                    //need to re-sync
                    return this.loadSounds();
                }

                let parentDirectory = this.getFile(file.parentDirectory);

                if (event.event == SoundboxFileChangeEventType.ADDED) {
                    //add to local database
                    parentDirectory.children.push(file);
                    file.parentDirectory = parentDirectory;

                    this.updateWatermark(file, watermarkNow);
                }
                else if (event.event == SoundboxFileChangeEventType.MODIFIED) {
                    //update local properties
                    let fileLocal = this.getFile(file);

                    fileLocal.iconUrl = file.iconUrl;
                    fileLocal.name = file.name;
                    fileLocal.tags = file.tags;
                    if (isSound(file) && isSound(fileLocal)) {
                        fileLocal.metaData = file.metaData;
                        fileLocal.voiceActivation = file.voiceActivation;
                    }

                    this.updateWatermark(fileLocal, watermarkNow);
                }
                else if (event.event == SoundboxFileChangeEventType.DELETED) {
                    //delete from local directory
                    let deleteDirectory = parentDirectory;

                    for (let i = 0; i < deleteDirectory.children.length; ++i) {
                        let testFile = deleteDirectory.children[i];

                        if (testFile.id == file.id) {
                            //found it => delete
                            deleteDirectory.children.splice(i, 1);
                            break;
                        }
                    }

                    this.updateWatermark(parentDirectory, watermarkNow);
                }
                else if (event.event == SoundboxFileChangeEventType.MOVED) {
                    //delete from local directory
                    let deleteDirectory = this.getFile(
                        (event as ISoundboxFileMoveEvent).fromDirectory
                    );

                    let deleted: ISoundboxFile;
                    for (let i = 0; i < deleteDirectory.children.length; ++i) {
                        let testFile = deleteDirectory.children[i];

                        if (testFile.id == file.id) {
                            //found it => delete
                            deleted = testFile;
                            deleteDirectory.children.splice(i, 1);
                            break;
                        }
                    }

                    //add deleted to new directory
                    parentDirectory.children.push(deleted);
                    deleted.parentDirectory = parentDirectory;

                    this.updateWatermark(deleteDirectory, watermarkNow);
                    this.updateWatermark(deleted, watermarkNow);
                }
            })
            .then(() => this.onSoundListUpdated());
    }

    /**
     * Called whenever the content of {@link #currentSoundsTree} has changed and is now in a valid state.
     **/
    protected onSoundListUpdated() {
        //update clients
        let sounds = this.getSoundsFromDirectoryLocal(this.currentSoundsTree);
        sounds.sort((a, b) => {
            return a.name.toLocaleLowerCase().localeCompare(b.name.toLocaleLowerCase());
        });

        this.soundsSubject.next(sounds);
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
    public upload(file: File, sound?: ISoundUpload, directory?: ISoundboxDirectory): IUploadStatus {

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

            if (!sound) {
                sound = {};
            }

            //add the file name to the request
            const soundFull: ISoundUploadFull = {
                ...sound,
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

    //#region Edit

    /**
     * Edits the given file (sound, directory, macro...) on the server.
     * Causes an EDIT event to be raised, for example via {@link sounds}<br/>
     * Currently, these properties are affected:<ul>
     *      <li>{@link ISoundboxFile#name}</li>
     *      <li>{@link ISoundboxFile#tags}</li>
     *      <li>{@link ISound#voiceActivation}</li>
     * </ul>
     * @param file
     * @return Promise that resolves with the complete, updated data from the server. Throws a {@link SoundboxError} on failure.
     */
    public edit<T extends ISoundboxFile>(file: T): Promise<T> {
        return this.connection.invoke("Edit", this.flattenFile(file))
            .catch(error => {
                console.error("Could not edit a sound", error);
                throw new SoundboxError({
                    code: ResultStatusCode.CONNECTION_ERROR
                });
            })
            .then((result: IFileResult) => {
                if (!result.success)
                    throw new SoundboxError(result.status);
                return result.file as T;
            });
    }

    //#region Delete

    /**
     * Deletes the given file (sound, directoy, macro...) from the server.
     * Causes a DELETE event to be raised, for example via {@link sounds}
     * @param file
     * @returns Resolves when the file has been deleted successfully. Throws a {@link SoundboxError} on failure.
     */
    public delete(file: ISoundboxFileBase): Promise<void> {
        return this.connection.invoke("Delete", this.minimizeFile(file))
            .catch(error => {
                console.error("Could not delete a sound", error);
                throw new SoundboxError({
                    code: ResultStatusCode.CONNECTION_ERROR
                });
            })
            .then((result: IServerResult) => {
                if (!result.success)
                    throw new SoundboxError(result.status);
            });
    }

    //#endregion

    //#endregion

    //#region Speech recognition

    /**
     * Uploads some recorded audio and runs it through the soundbox's speech recognition. People can be hard to understand and speech recognition
     * isn't always an exact science. Thus, this can help the user to get a feeling for how the speech recognition works:
     * they can record some audio with their microphone, upload it, and they'll get the exact text output of the speech recognizer along with feedback
     * if their entered triggers have been detected successfully.
     * @param audio Audio blob. Either a file selected from disk or audio recorded via {@link MediaRecorder}. Should ideally be of type "audio/webm; codecs=opus"
     * @param recognizables List of recognizables: Each contains a list of "triggers". The soundbox will match the transcribed text against these triggers and will then return
     *          the matching recognizable in the result events.
     * @param phrases List of special phrases that are probably hard to detect for the speech recognition. See also {@link ISoundboxVoiceActivation#speechPhrases}
     */
    public testSpeechRecognition(audio: Blob, recognizables?: ISound[] | ISpeechRecognitionTestRecognizable[], phrases?: ISoundboxPlayable[] | string[]): Observable<SpeechRecognitionTestResult> {
        let subject = new Subject<SpeechRecognitionTestResult>();

        //convert parameters
        let paramRecognizables: ISpeechRecognitionTestRecognizable[] = [];
        let paramPhrases: string[] = [];

        if (recognizables?.length) {
            let test = recognizables[0];
            if ((test as ISound).voiceActivation) {
                //is a list of sounds
                for (let sound of recognizables as ISound[]) {
                    if (!sound?.voiceActivation?.speechTriggers?.length)
                        continue;

                    paramRecognizables.push({
                        id: sound.id,
                        speechTriggers: sound.voiceActivation.speechTriggers
                    });
                }
            }
            else {
                //is a list of recognizables
                paramRecognizables = recognizables as ISpeechRecognitionTestRecognizable[];
            }
        }

        if (phrases?.length) {
            let test = phrases[0];
            if (test instanceof String) {
                //plain strings
                paramPhrases = phrases as string[];
            }
            else {
                //playables (probably sounds)
                for (let playable of phrases as ISoundboxPlayable[]) {
                    if (!playable?.voiceActivation?.speechPhrases?.length)
                        continue;

                    paramPhrases.push(...playable.voiceActivation.speechPhrases);
                }
            }
        }

        //read the audio blob and upload as base64 (not exactly efficient, but good enough for short clips)
        let reader = new FileReader();

        reader.onload = () => {
            let base64 = (reader.result as string).split(",", 2)[1];

            this.connection.stream("TestSpeechRecognition", base64, audio.type, paramRecognizables, paramPhrases).subscribe({
                next: value => subject.next(value),
                complete: () => subject.complete(),
                error: err => subject.error(err)
            });
        };
        reader.readAsDataURL(audio);

        return subject;
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

    /**
     * Returns a copy of the given file that does not reference its parent or its children (for directories) anymore.
     * This results in a minimized-ish version of the file that can be efficiently uploaded to edit files.
     * @param file
     */
    protected flattenFile<T extends ISoundboxFile>(file: T): T {
        let copy: T = {
            ...file
        };
        //flatten:
        delete copy.parentDirectory;
        if (isDirectory(copy)) {
            copy.children = [];
        }

        return copy;
    }

    //#endregion
}