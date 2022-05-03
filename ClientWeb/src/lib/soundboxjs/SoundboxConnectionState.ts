export enum SoundboxConnectionState {
    /**
     * The soundbox is disconnected as {@link Soundbox#start} hasn't been called yet.
     **/
    Initial,
    /**
     * The soundbox is trying to establish the initial connection after {@link Soundbox#start} has been called.
     **/
    Connecting,
    /**
     * The soundbox has lost the server connection but is automatically trying to reconnect.
     **/
    Reconnecting,
    /**
     * The soundbox is connected and should be fully operational.
     **/
    Connected,
    /**
     * The soundbox is disconnected and must be connected again manually (either by refreshing the page or calling {@link Soundbox#start}).
     **/
    Disconnected
}