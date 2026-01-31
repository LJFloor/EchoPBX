export interface OngoingCall {
    uniqueId: string;
    externalNumber: string;
    externalName: string | null;
    extensionNumber: number | null;
    extensionName: string | null;
    queueId: number | null;
    trunkId: number | null;
    startTime: number;
    pickupTime: number | null;
    direction: CallDirection;
    state: CallState;
}

export enum CallDirection {
    Incoming = 1,
    Outgoing = 2,
    Internal = 3,
}

export enum CallState {
    Ringing = 1,
    Ongoing = 2
}