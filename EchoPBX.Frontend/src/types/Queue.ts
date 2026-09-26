export interface Queue {
    id: number;
    name: string;
    strategy: string;
    timeout: number;
    maxLength: number;
    wrapUpTime: number;
    retryInterval: number;
    musicOnHold: string[];
    announcement?: string;
    extensions: number[];
}