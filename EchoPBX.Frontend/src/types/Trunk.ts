export interface Trunk {
    id: number;
    name: string;
    host: string;
    username: string;
    password: string;
    cid?: string;
    codecs: string[];
    connected: boolean;
    extensions: number[];
    queueId?: number;
    incomingCallBehaviour: IncomingCallBehaviour;
    dtmfAnnouncement?: string;
    dtmfMenuEntries: DtmfMenuEntry[];
}

export interface DtmfMenuEntry {
    digit: number;
    queueId: number;
    label?: string;
}

export enum IncomingCallBehaviour {
    Ignore = 1,
    RingAllExtensions = 2,
    RingSpecificExtensions = 3,
    RingQueue = 4,
    DtmfMenu = 5
}