import { ResultStatusCode } from './ResultStatusCode';

export interface IResultStatus {
    code: ResultStatusCode,
    message?: string
}