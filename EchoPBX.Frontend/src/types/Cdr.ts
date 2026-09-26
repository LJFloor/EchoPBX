import type { CallDirection } from './OngoingCall';

export interface CdrEntry {
    id: number;
    source: string;
    destination: string;
    direction: CallDirection;

    /** Unix timestamps in milliseconds */
    start: number;
    answer: number | null;
    end: number;

    /** In seconds, ringing included */
    duration: number;

    /** In seconds, from the moment the call was answered */
    billSeconds: number;
    disposition: CdrDisposition;
}

/**
 * Keep in sync with EchoPBX.Data.Models.CdrDisposition
 */
export enum CdrDisposition {
    Answered = 1,
    NoAnswer = 2,
    Busy = 3,
}
