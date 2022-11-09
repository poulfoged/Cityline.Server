import EventTarget from "@ungap/event-target";

import { CitylineOptions } from "./CitylineOptions";
import { Protocol, StateAccessor } from "./Protocol";
import { WebsocketProtocol } from "./WebsocketProtocol";

export class CitylineClient implements StateAccessor {
    getState = (): CitylineRequest => <CitylineRequest>({ tickets: this._idCache });
    private eventTarget = new EventTarget();
    private _citylineOptions: CitylineOptions;
    private _frames: { [key: string]: Frame } = {};
    private _idCache = {};
    private _currentProtocol: Protocol;
    private _protocols = {
        "sockets": (citylineOptions: CitylineOptions, stateAccessor: StateAccessor) => new WebsocketProtocol(citylineOptions, stateAccessor),
        //"fetch": (citylineOptions: CitylineOptions, stateAccessor: StateAccessor) => new FetchStreamProtocol(citylineOptions, stateAccessor),
        //"xhr": (citylineOptions: CitylineOptions, stateAccessor: StateAccessor) => new XhrStreamProtocol(citylineOptions, stateAccessor),
    };

    constructor(private citylineOptionsFunc: () => CitylineOptions) {
        //window.setTimeout(this.doStart);
    }

    public start() { 
        this._citylineOptions = this.citylineOptionsFunc();
        window.setTimeout(this.doStart);
    }

    public async send<T>(event: string, data: T) : Promise<boolean> { 
        var frame: Frame = {
            event: event,
            data: JSON.stringify(data)
        };

        return await this._currentProtocol.send(frame);
    }

    private disconnectHandler = () => this.eventTarget.dispatchEvent(new CustomEvent("disconnect"));
    
    private connectHandler = () => this.eventTarget.dispatchEvent(new CustomEvent("connect"));

    private doStart = async () => { 
        let protocolNumber = 0; 
        let protocolName: string;
    
        /*eslint no-constant-condition: ["error", { "checkLoops": false }]*/
        while (true) { 
            try {
                protocolName = Object.keys(this._protocols)[protocolNumber];
                //console.log(`Starting protocol ${protocolName}`);
                this._currentProtocol = this._protocols[protocolName](this._citylineOptions, this);
                this._currentProtocol.onMessage(this.addFrame);
                this._currentProtocol.onDisconnect(this.disconnectHandler);
                this._currentProtocol.onConnect(this.connectHandler);
                await this._currentProtocol.start();
            }
            catch (error) {
                console.log(`Protocol ${protocolName} failed`, error);

                protocolNumber++;
                if (protocolNumber > Object.keys(this._protocols).length - 1)
                    protocolNumber = 0;
            } finally { 
                this._currentProtocol.destroy();
            }

           // TODO: delay?
        }
    }

    addEventListener(
        type: string,
        listener: EventListenerOrEventListenerObject,
        options?: boolean | AddEventListenerOptions
    ) {
        return this.eventTarget.addEventListener(type, listener, options);
    }

    removeEventListener(
        type: string,
        listener: EventListenerOrEventListenerObject,
        options?: boolean | EventListenerOptions
    ) {
        this.eventTarget.removeEventListener(type, listener, options);
    }

    


    private addFrame = (frame: Frame) => {
        if (frame && frame.event) {
            if (frame.id)
                this._idCache[frame.event] = frame.id;

            this._frames[frame.event] = frame;

            setTimeout(() => {
                this.eventTarget.dispatchEvent(
                    new CustomEvent(frame.event, {
                        detail: frame.data
                    })
                );

                this.eventTarget.dispatchEvent(
                    new CustomEvent("frame-received", {
                        detail: frame
                    })
                );
            });
        }
    }

    async getFrame<T>(name: string): Promise<T> {
        if (this._frames[name])
            return this._frames[name].data;

        return new Promise<T>(r => {
            const handler = (event: CustomEvent<any>) => {
                this.removeEventListener(name, handler);
                r(event.detail);
            };
            this.addEventListener(name, handler);
        });
    }
}

interface CitylineRequest {
    tickets: { [key: string]: string };
}

export interface Frame {
    id?: string;
    event?: string;
    data: any;
}
