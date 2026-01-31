export interface Queue {
    id: number;
    name: string;
    strategy: string;
    timeout: number;
    maxlength: number;
    wrapUpTime: number;
    retryInterval: number;
    musicOnHold: string[];
    announcement?: string;
    extensions: number[];
}