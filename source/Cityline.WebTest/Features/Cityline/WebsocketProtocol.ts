import { Frame } from "./CitylineClient";
import { CitylineOptions } from "./CitylineOptions";
import { Protocol, StateAccessor } from "./Protocol"; 

enum WebsocketState {
    CONNECTING = 0, // Socket has been created. The connection is not yet open.
    OPEN = 1,       // The connection is open and ready to communicate.
    CLOSING = 2,    // The connection is in the process of closing.
    CLOSED = 3      // The connection is closed or couldn't be opened.
}

export class WebsocketProtocol implements Protocol {
    private socket: WebSocket;
    protected errorCallback: (error: any) => void;
    protected messageCallback: (data: Frame) => void;
    protected connectCallback: () => void;
    protected disconnectCallback: () => void;
    private abortPromise;

    constructor(protected options: CitylineOptions, protected stateAccessor: StateAccessor) { }
    onConnect(callback: () => void) {
        this.connectCallback = callback;
    }
    onDisconnect(callback: () => void) {
        this.disconnectCallback = callback;
    }

    onMessage(callback: (frame: Frame) => void) {
        this.messageCallback = callback;
    }

    onError(callback: (message: string) => void) {
        this.errorCallback = callback;
    }

    private messageHandler = (event: MessageEvent) => {
        const frame = JSON.parse(event.data);

        if (frame.data)
            frame.data = JSON.parse(frame.data);


        if (this.messageCallback)
            this.messageCallback(frame);
    }

    private errorHandler = (event: Event) => {
        if (this.errorCallback)
            this.errorCallback(event);
    }

    private pingSender = async () => {
        if (this.socket.readyState === 1) {
            const frame: Frame = {
                event: "_ping",
                data: JSON.stringify("ping")
            };
            await this.send(frame);
        }

        setTimeout(this.pingSender, 30000);
    }

    private static _reconnectAttempts = 0;

    public start = async () => {
        try {
            this.socket = new WebSocket(this.options.socketEndpoint);

            window.addEventListener("unload", () => {
                if( this.socket?.readyState == WebsocketState.OPEN)
                    this.socket?.close();
            });

            this.socket.addEventListener("message", this.messageHandler);
            this.socket.addEventListener("error", this.errorHandler);
            const status = await this.waitForStatus(status => status === 1, 3000);

            if (status === WebsocketState.OPEN) {
                WebsocketProtocol._reconnectAttempts = 0;
            }

            this.connectCallback?.call(this);

            // always send headers first (auth etc.)
            const frame: Frame = {
                event: "_headers",
                data: JSON.stringify(this.options.headers || {})
            };
            console.log("Sending headers");
            await this.send(frame);

            // now send state of all channels
            const request: Frame = {
                event: "_request",
                data: JSON.stringify(this.stateAccessor.getState())
            };
            await this.send(request);

            // ping sender
            await this.pingSender();

            // wait for error or disconnect
            await new Promise((resolve, reject) => {
                this.socket.addEventListener("error", () => {
                    console.log("Websocket error");
                    reject(true);
                }, { once: true });
                this.socket.addEventListener("close", () => {
                    console.log("Websocket closed");
                    resolve(true);
                }, { once: true });
            });
        } catch (error) {
            WebsocketProtocol._reconnectAttempts++;

            console.log("failed attempts: " + WebsocketProtocol._reconnectAttempts);

            if (WebsocketProtocol._reconnectAttempts > 3) {
                this.disconnectCallback?.call(this);
                WebsocketProtocol._reconnectAttempts = 0;
            }
        } finally {
            setTimeout(() => this.start, 1000);
        }
    }

    private async waitForStatus(condition: (status: number) => boolean, timeout?: number): Promise<number> {
        let timeoutHandler;
        return await new Promise((resolve, reject) => {
            const check = () => {
                if (condition(this.socket.readyState)) {
                    window.clearTimeout(timeoutHandler);
                    resolve(this.socket.readyState);
                }

                setTimeout(check, 1000);
            };

            if (timeout)
                timeoutHandler = setTimeout(() => {
                    console.log(`Timeout waiting for status: ${condition}.`);
                    reject();
                }, timeout);

            check();
        });
    }

    send(data: Frame): Promise<boolean> {
        if (this.socket.readyState !== this.socket.OPEN)
            return Promise.resolve(false);

        try {
            this.socket.send(JSON.stringify(data));
        } catch (error) {
            return Promise.resolve(false);
        }

        return Promise.resolve(true);
    }

    destroy() {
        this.socket?.removeEventListener("message", this.messageHandler);
        this.socket?.removeEventListener("error", this.errorHandler);

        if (this.socket?.readyState === 1)
            this.socket.close();
    }
}
