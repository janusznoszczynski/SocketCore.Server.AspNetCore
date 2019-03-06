function clone(obj) {
    return JSON.parse(JSON.stringify(obj));
}

function createCookie(name: string, value: string, days: number) {
    let expires = "";

    if (days) {
        const date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toUTCString();
    }

    document.cookie = name + "=" + value + expires + "; path=/";
}

function readCookie(name: string) {
    const nameEQ = name + "=";
    const ca = document.cookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
}

function eraseCookie(name: string) {
    createCookie(name, "", -1);
}

function guid() {
    function s4() {
        return Math.floor((1 + Math.random()) * 0x10000).toString(16).substring(1);
    }

    return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
}

function prepareUrl(urlOrPath: string) {
    if (urlOrPath.indexOf("ws://") !== -1 || urlOrPath.indexOf("wss://") !== -1) {
        return urlOrPath;
    }

    if (location.protocol === 'https:') {
        return "wss://" + location.host + urlOrPath;
    }

    return "ws://" + location.host + urlOrPath;
}


export type EventHandler = (sender: any, arg: any) => void;

export class SocketEvent {
    handlers: EventHandler[] = [];

    subscribe(fn: EventHandler) {
        this.handlers.push(fn);
    }

    unsubscribe(fn: EventHandler) {
        this.handlers = this.handlers.filter((item) => {
            if (item !== fn) {
                return item;
            }
        });
    }

    fire(o, thisObj) {
        const scope = thisObj || window;
        this.handlers.forEach((item) => {
            item.call(scope, o);
        });
    }
}


export enum ConnectionState {
    Opening = 0,
    Opened = 1,
    Reopening = 2,
    Closed = 3
};

export class Connection {
    url: string = null;
    webSocket: WebSocket = null;
    connectionId: string = null;
    state = null;

    openingEvt: SocketEvent = new SocketEvent();
    openedEvt: SocketEvent = new SocketEvent();
    reopeningEvt: SocketEvent = new SocketEvent();
    closedEvt: SocketEvent = new SocketEvent();
    stateChangedEvt: SocketEvent = new SocketEvent();
    recievedEvt: SocketEvent = new SocketEvent();
    sentEvt: SocketEvent = new SocketEvent();
    errorEvt: SocketEvent = new SocketEvent();

    constructor(urlOrPath: string) {
        this.url = prepareUrl(urlOrPath);
    }

    opening(handler: EventHandler) {
        this.openingEvt.subscribe(handler);
    }

    opened(handler: EventHandler) {
        this.openedEvt.subscribe(handler);
    }

    reopening(handler: EventHandler) {
        this.reopeningEvt.subscribe(handler);
    }

    closed(handler: EventHandler) {
        this.closedEvt.subscribe(handler);
    }

    recieved(handler: EventHandler) {
        this.recievedEvt.subscribe(handler);
    }

    sent(handler: EventHandler) {
        this.sentEvt.subscribe(handler);
    }

    stateChanged(handler: EventHandler) {
        this.stateChangedEvt.subscribe(handler);
    }

    error(handler: EventHandler) {
        this.errorEvt.subscribe(handler);
    }

    fireStateChanged(newState) {
        const previousState = this.state;
        this.state = newState;

        this.stateChangedEvt.fire({ current: newState, previous: previousState }, this);

        switch (newState) {
            case ConnectionState.Opening:
                this.openingEvt.fire(ConnectionState.Opening, this);
                break;

            case ConnectionState.Opened:
                this.openedEvt.fire(ConnectionState.Opened, this);
                break;

            case ConnectionState.Reopening:
                this.reopeningEvt.fire(ConnectionState.Reopening, this);
                break;

            case ConnectionState.Closed:
                this.closedEvt.fire(ConnectionState.Closed, this);
                break;
        }
    }

    open() {
        this.fireStateChanged(ConnectionState.Opening);

        this.webSocket = new WebSocket(this.url);

        this.webSocket.onopen = (evt) => {
        };

        this.webSocket.onmessage = (evt) => {
            const cmd = JSON.parse(evt.data);

            if (cmd.Type === "SetConnectionId") {
                this.connectionId = cmd.Data;
                this.fireStateChanged(ConnectionState.Opened);
            }
            else if (cmd.Type === "Data") {
                this.recievedEvt.fire(cmd.Data, this);
            }
        };

        this.webSocket.onerror = (evt) => {
            this.errorEvt.fire(evt, this);
        };

        this.webSocket.onclose = (evt) => {
            this.fireStateChanged(ConnectionState.Closed);
        };
    }

    send(data: any) {
        window.onbeforeunload = () => {
            this.webSocket.onclose = () => null; // disable onclose handler first
            this.webSocket.close()
        };

        if (this.webSocket.readyState === WebSocket.OPEN) {
            const cmd = { Type: "Data", Data: data };
            this.webSocket.send(JSON.stringify(cmd));
            this.sentEvt.fire(data, this);
        }
        else if (this.webSocket.readyState === WebSocket.CONNECTING) {
            const handle = setTimeout(() => {
                clearTimeout(handle);
                this.send(data);
            }, 100);
        }
        else if (this.webSocket.readyState === WebSocket.CLOSING) {
            this.open();
            this.fireStateChanged(ConnectionState.Reopening);

            const handle = setTimeout(() => {
                clearTimeout(handle);
                this.send(data);
            }, 100);
        }
        else if (this.webSocket.readyState === WebSocket.CLOSED) {
            this.open();
            this.fireStateChanged(ConnectionState.Reopening);

            const handle = setTimeout(() => {
                clearTimeout(handle);
                this.send(data);
            }, 100);
        }
    }

    close(code?: null, reason?: string) {
        this.webSocket.close(code, reason);
    }
}


export class WorkflowClient {
    connection: Connection;
    sessionId: string;
    senderId: string;
    handlers: EventHandler[];
    handlersWorkflowsEvents: EventHandler[];
    options: any;

    constructor(urlOrPath: string, options: any) {
        options = options || {};

        options.contextHeader = options.contextHeader || (() => window.location.hash.replace("#", ""));
        options.contextHeaderEnabled = options.contextHeaderEnabled === undefined ? true : options.contextHeaderEnabled;

        this.connection = new Connection(urlOrPath);
        this.sessionId = options.sessionId || this.generateSessionId();
        this.senderId = options.senderId || "WebClient";
        this.handlers = [];
        this.handlersWorkflowsEvents = [];
        this.options = options;

        this.connection.opening(() => {
            this.dispatchWorkflowsEventsMessage(new OpeningMessage(), this);
        });

        this.connection.reopening(() => {
            this.dispatchWorkflowsEventsMessage(new ReopeningMessage(this.connection.connectionId), this);
        });

        this.connection.closed(() => {
            this.dispatchWorkflowsEventsMessage(new ClosedMessage(this.connection.connectionId), this);
        });

        this.connection.stateChanged((state) => {
            this.dispatchWorkflowsEventsMessage(new StateChangedMessage(state), this);
        });

        this.connection.recieved((data) => {
            this.dispatchMessage(data, this);
        });

        this.connection.sent((data) => {
            this.dispatchWorkflowsEventsMessage(new SentMessage(data), this);
        });

        this.connection.error((error) => {
            this.dispatchWorkflowsEventsMessage(new ErrorMessage(error), this);
        });
    }

    generateSessionId() {
        let sessionId = readCookie("SocketCore.SessionId");

        if (!sessionId) {
            sessionId = guid();
            createCookie("SocketCore.SessionId", sessionId, 0);
        }

        return sessionId;
    }

    run(handler: EventHandler) {
        this.connection.opened(handler);
        this.connection.open();
    }

    send(channel: string, message: Message) {
        const options = this.options;

        if (!message.messageId) {
            message.messageId = guid();
        }

        message.connectionId = this.connection.connectionId;
        message.senderId = this.senderId;
        message.sessionId = this.sessionId;

        if (!Array.isArray(message.headers)) {
            message.headers = [];
        }

        if (options.contextHeaderEnabled) {
            message.headers.push(new MessageHeader("Context", options.contextHeader()));
        }

        this.connection.send({
            Channel: channel,
            Message: message
        });
    }

    subscribe(fn: EventHandler) {
        this.handlers.push(fn);
    }

    unsubscribe(fn: EventHandler) {
        this.handlers = this.handlers.filter(
            function (item) {
                if (item !== fn) {
                    return item;
                }
            }
        );
    }

    dispatchMessage(data, thisObj) {
        this.handlers.forEach((handler) => {
            if (data instanceof Array) {
                data.forEach(function (item) {
                    var msg = clone(item); // copy of the message
                    handler.call(thisObj, msg);
                });
            }
            else {
                var msg = clone(data); // copy of the message
                handler.call(thisObj, msg);
            }
        });
    }

    subscribeWorkflowsEventsChannel(fn: EventHandler) {
        this.handlersWorkflowsEvents.push(fn);
    }

    unsubscribeWorkflowsEventsChannel(fn: EventHandler) {
        this.handlersWorkflowsEvents = this.handlersWorkflowsEvents.filter((item) => {
            if (item !== fn) {
                return item;
            }
        });
    }

    dispatchWorkflowsEventsMessage(msg, thisObj) {
        this.handlersWorkflowsEvents.forEach((item) => {
            const message = clone(msg); // copy of the message
            item.call(thisObj, message);
        });
    }
}

export class Message {
    namespace: string;
    type: string;
    data: any;
    headers: MessageHeader[];

    messageId: string = null;
    connectionId: string = null;
    sessionId: string = null;
    senderId: string = null;
    replyToMessageId: string = null;

    constructor(ns: string, type: string, data: any = null, headers: MessageHeader[] = null) {
        this.namespace = ns;
        this.type = type;
        this.data = data;
        this.headers = headers || [];

        this.messageId = null;
        this.connectionId = null;
        this.sessionId = null;
        this.senderId = null;
        this.replyToMessageId = null;
    }

    isMatch(ns: string, type: string) {
        return this.namespace === ns && this.type === type;
    }

    // isMatch(msg: Message) {
    //     return this.namespace === msg.namespace && this.type === msg.type;
    // }
}

export class MessageHeader {
    name: string;
    value: string;

    constructor(name: string, value: string) {
        this.name = name;
        this.value = value;
    }
}

export class OpeningMessage extends Message {
    constructor() {
        super("SocketCore.WokrflowEvents", "Opening");
    }
}

export class ReopeningMessage extends Message {
    constructor(connectionId: string) {
        super("SocketCore.WokrflowEvents", "Reopening", connectionId);
    }
}

export class ClosedMessage extends Message {
    constructor(connectionId: string) {
        super("SocketCore.WokrflowEvents", "Closed", connectionId);
    }
}

export class StateChangedMessage extends Message {
    constructor(state: any) {
        super("SocketCore.WokrflowEvents", "StateChanged", state);
    }
}

export class SentMessage extends Message {
    constructor(data: any) {
        super("SocketCore.WokrflowEvents", "Sent", data);
    }
}

export class ErrorMessage extends Message {
    constructor(error: any) {
        super("SocketCore.WokrflowEvents", "Error", error);
    }
}



//back campatibility with old JS client
export { EventHandler as eventHandler };
export { SocketEvent as socketEvent };
export { ConnectionState as connectionState };
export { Connection as connection };
export { WorkflowClient as workflowClient };
export { Message as message };
export { MessageHeader as messageHeader };