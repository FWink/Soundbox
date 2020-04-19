import { IResultStatus } from '../results/ResultStatus';
import { ResultStatusCode } from '../results/ResultStatusCode';

export class SoundboxError extends Error {

    public readonly code: ResultStatusCode;

    constructor(status: IResultStatus) {
        super(status.message);
        this.code = status.code;
    }
}