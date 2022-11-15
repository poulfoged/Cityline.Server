import { Frame } from "./CitylineClient";

export interface Protocol { 
    // initialize protocol, only returns on fail
    start(): Promise<void>;
    send(data: Frame): Promise<boolean>;
    destroy();

    onMessage(callback: (data: Frame) => void);
    onError(callback: (message: string) => void);
    onConnect(callback: () => void);
    onDisconnect(callback: () => void);
}

export interface StateAccessor { 
    getState(): any;
}