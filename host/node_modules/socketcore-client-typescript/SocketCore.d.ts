export declare type EventHandler = (sender: any, arg: any) => void;
export declare class SocketEvent {
    handlers: EventHandler[];
    subscribe(fn: EventHandler): void;
    unsubscribe(fn: EventHandler): void;
    fire(o: any, thisObj: any): void;
}
export declare enum ConnectionState {
    Opening = 0,
    Opened = 1,
    Reopening = 2,
    Closed = 3
}
export declare class Connection {
    url: string;
    webSocket: WebSocket;
    connectionId: string;
    state: any;
    openingEvt: SocketEvent;
    openedEvt: SocketEvent;
    reopeningEvt: SocketEvent;
    closedEvt: SocketEvent;
    stateChangedEvt: SocketEvent;
    recievedEvt: SocketEvent;
    sentEvt: SocketEvent;
    errorEvt: SocketEvent;
    constructor(urlOrPath: string);
    opening(handler: EventHandler): void;
    opened(handler: EventHandler): void;
    reopening(handler: EventHandler): void;
    closed(handler: EventHandler): void;
    recieved(handler: EventHandler): void;
    sent(handler: EventHandler): void;
    stateChanged(handler: EventHandler): void;
    error(handler: EventHandler): void;
    fireStateChanged(newState: any): void;
    open(): void;
    send(data: any): void;
    close(code?: null, reason?: string): void;
}
export declare class WorkflowClient {
    connection: Connection;
    sessionId: string;
    senderId: string;
    handlers: EventHandler[];
    handlersWorkflowsEvents: EventHandler[];
    options: any;
    constructor(urlOrPath: string, options: any);
    generateSessionId(): string;
    run(handler: EventHandler): void;
    send(channel: string, message: Message): void;
    subscribe(fn: EventHandler): void;
    unsubscribe(fn: EventHandler): void;
    dispatchMessage(data: any, thisObj: any): void;
    subscribeWorkflowsEventsChannel(fn: EventHandler): void;
    unsubscribeWorkflowsEventsChannel(fn: EventHandler): void;
    dispatchWorkflowsEventsMessage(msg: any, thisObj: any): void;
}
export declare class Message {
    namespace: string;
    type: string;
    data: any;
    headers: MessageHeader[];
    messageId: string;
    connectionId: string;
    sessionId: string;
    senderId: string;
    replyToMessageId: string;
    constructor(ns: string, type: string, data?: any, headers?: MessageHeader[]);
    isMatch(ns: string, type: string): boolean;
}
export declare class MessageHeader {
    name: string;
    value: string;
    constructor(name: string, value: string);
}
export declare class OpeningMessage extends Message {
    constructor();
}
export declare class ReopeningMessage extends Message {
    constructor(connectionId: string);
}
export declare class ClosedMessage extends Message {
    constructor(connectionId: string);
}
export declare class StateChangedMessage extends Message {
    constructor(state: any);
}
export declare class SentMessage extends Message {
    constructor(data: any);
}
export declare class ErrorMessage extends Message {
    constructor(error: any);
}
export { EventHandler as eventHandler };
export { SocketEvent as socketEvent };
export { ConnectionState as connectionState };
export { Connection as connection };
export { WorkflowClient as workflowClient };
export { Message as message };
export { MessageHeader as messageHeader };
