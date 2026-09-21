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
    callFlowId?: number | null;
}

export enum IncomingCallBehaviour {
    Ignore = 1,
    RingAllExtensions = 2,
    RingSpecificExtensions = 3,
    RingQueue = 4,
    SendToCallFlow = 6,
}