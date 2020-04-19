export enum ResultStatusCode {
    //base
    OK = 0,
    OK_NO_CHANGE = -1,

    INVALID_PARAMETER = -400,
    ACCESS_DENIED = -403,
    PERMISSION_DENIED = -461,
    RESSOURCE_DENIED = -462,

    INTERNAL_SERVER_ERROR = -500,

    UNKNOWN_ERROR = -1000,
    CONNECTION_ERROR = -1001,

    //files
    INVALID_DISPLAY_NAME_EXISTS = 1400,
    ILLEGAL_FILE_TYPE = 1401,
    FILE_DOES_NOT_EXIST = 1402,
    INVALID_FILE_NAME = 1403,
    ILLEGAL_FILE_EDIT_DENIED = 1404,
    ILLEGAL_FILE_EDIT_DENIED_ROOT = 1405,
    MOVE_TARGET_INVALID = 1406,

    IO_ERROR = 1500,

    //volume
    INVALID_VOLUME_MAX = 2400,
    INVALID_VOLUME_MIN = 2401,
}

/**
 * Attempts to translate the given HTTP status code into a {@link ResultStatusCode}.
 * @param statusCode
 */
export function fromHttp(statusCode: number) {
    switch (statusCode) {
        case 500:
            return ResultStatusCode.INTERNAL_SERVER_ERROR;
    }

    return ResultStatusCode.UNKNOWN_ERROR;
}