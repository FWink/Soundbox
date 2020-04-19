import { ISound } from './Sound';

export interface IPlayingNow {
    sound: ISound;
    user: IPlayingNowFromUser;
}

export interface IPlayingNowFromUser {
    name: string;
}