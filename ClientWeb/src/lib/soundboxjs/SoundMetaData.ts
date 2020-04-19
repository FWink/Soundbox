export interface ISoundMetaData {
    /**
     * The sound's play length at 100% speed in ms.
     * */
    length: number;

    /**
     * True if the meta data contains a valid {@link Length}
     * */
    hasLength: boolean;
}