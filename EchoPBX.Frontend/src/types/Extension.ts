export interface Extension {
    extensionNumber: number;
    displayName?: string;
    email?: string;
    password: string;
    connected: boolean;
    outgoingTrunkId: number | null;
}

export enum ContactStatus {
    Available = 1,
    NonQualify = 2,
    Unavailable = 3
}