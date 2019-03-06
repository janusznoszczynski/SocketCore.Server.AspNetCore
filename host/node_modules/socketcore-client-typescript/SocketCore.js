"use strict";
var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
        return extendStatics(d, b);
    }
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
function clone(obj) {
    return JSON.parse(JSON.stringify(obj));
}
function createCookie(name, value, days) {
    var expires = "";
    if (days) {
        var date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toUTCString();
    }
    document.cookie = name + "=" + value + expires + "; path=/";
}
function readCookie(name) {
    var nameEQ = name + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ')
            c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) == 0)
            return c.substring(nameEQ.length, c.length);
    }
    return null;
}
function eraseCookie(name) {
    createCookie(name, "", -1);
}
function guid() {
    function s4() {
        return Math.floor((1 + Math.random()) * 0x10000).toString(16).substring(1);
    }
    return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
}
function prepareUrl(urlOrPath) {
    if (urlOrPath.indexOf("ws://") !== -1 || urlOrPath.indexOf("wss://") !== -1) {
        return urlOrPath;
    }
    if (location.protocol === 'https:') {
        return "wss://" + location.host + urlOrPath;
    }
    return "ws://" + location.host + urlOrPath;
}
var SocketEvent = /** @class */ (function () {
    function SocketEvent() {
        this.handlers = [];
    }
    SocketEvent.prototype.subscribe = function (fn) {
        this.handlers.push(fn);
    };
    SocketEvent.prototype.unsubscribe = function (fn) {
        this.handlers = this.handlers.filter(function (item) {
            if (item !== fn) {
                return item;
            }
        });
    };
    SocketEvent.prototype.fire = function (o, thisObj) {
        var scope = thisObj || window;
        this.handlers.forEach(function (item) {
            item.call(scope, o);
        });
    };
    return SocketEvent;
}());
exports.SocketEvent = SocketEvent;
exports.socketEvent = SocketEvent;
var ConnectionState;
(function (ConnectionState) {
    ConnectionState[ConnectionState["Opening"] = 0] = "Opening";
    ConnectionState[ConnectionState["Opened"] = 1] = "Opened";
    ConnectionState[ConnectionState["Reopening"] = 2] = "Reopening";
    ConnectionState[ConnectionState["Closed"] = 3] = "Closed";
})(ConnectionState = exports.ConnectionState || (exports.ConnectionState = {}));
exports.connectionState = ConnectionState;
;
var Connection = /** @class */ (function () {
    function Connection(urlOrPath) {
        this.url = null;
        this.webSocket = null;
        this.connectionId = null;
        this.state = null;
        this.openingEvt = new SocketEvent();
        this.openedEvt = new SocketEvent();
        this.reopeningEvt = new SocketEvent();
        this.closedEvt = new SocketEvent();
        this.stateChangedEvt = new SocketEvent();
        this.recievedEvt = new SocketEvent();
        this.sentEvt = new SocketEvent();
        this.errorEvt = new SocketEvent();
        this.url = prepareUrl(urlOrPath);
    }
    Connection.prototype.opening = function (handler) {
        this.openingEvt.subscribe(handler);
    };
    Connection.prototype.opened = function (handler) {
        this.openedEvt.subscribe(handler);
    };
    Connection.prototype.reopening = function (handler) {
        this.reopeningEvt.subscribe(handler);
    };
    Connection.prototype.closed = function (handler) {
        this.closedEvt.subscribe(handler);
    };
    Connection.prototype.recieved = function (handler) {
        this.recievedEvt.subscribe(handler);
    };
    Connection.prototype.sent = function (handler) {
        this.sentEvt.subscribe(handler);
    };
    Connection.prototype.stateChanged = function (handler) {
        this.stateChangedEvt.subscribe(handler);
    };
    Connection.prototype.error = function (handler) {
        this.errorEvt.subscribe(handler);
    };
    Connection.prototype.fireStateChanged = function (newState) {
        var previousState = this.state;
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
    };
    Connection.prototype.open = function () {
        var _this = this;
        this.fireStateChanged(ConnectionState.Opening);
        this.webSocket = new WebSocket(this.url);
        this.webSocket.onopen = function (evt) {
        };
        this.webSocket.onmessage = function (evt) {
            var cmd = JSON.parse(evt.data);
            if (cmd.Type === "SetConnectionId") {
                _this.connectionId = cmd.Data;
                _this.fireStateChanged(ConnectionState.Opened);
            }
            else if (cmd.Type === "Data") {
                _this.recievedEvt.fire(cmd.Data, _this);
            }
        };
        this.webSocket.onerror = function (evt) {
            _this.errorEvt.fire(evt, _this);
        };
        this.webSocket.onclose = function (evt) {
            _this.fireStateChanged(ConnectionState.Closed);
        };
    };
    Connection.prototype.send = function (data) {
        var _this = this;
        window.onbeforeunload = function () {
            _this.webSocket.onclose = function () { return null; }; // disable onclose handler first
            _this.webSocket.close();
        };
        if (this.webSocket.readyState === WebSocket.OPEN) {
            var cmd = { Type: "Data", Data: data };
            this.webSocket.send(JSON.stringify(cmd));
            this.sentEvt.fire(data, this);
        }
        else if (this.webSocket.readyState === WebSocket.CONNECTING) {
            var handle_1 = setTimeout(function () {
                clearTimeout(handle_1);
                _this.send(data);
            }, 100);
        }
        else if (this.webSocket.readyState === WebSocket.CLOSING) {
            this.open();
            this.fireStateChanged(ConnectionState.Reopening);
            var handle_2 = setTimeout(function () {
                clearTimeout(handle_2);
                _this.send(data);
            }, 100);
        }
        else if (this.webSocket.readyState === WebSocket.CLOSED) {
            this.open();
            this.fireStateChanged(ConnectionState.Reopening);
            var handle_3 = setTimeout(function () {
                clearTimeout(handle_3);
                _this.send(data);
            }, 100);
        }
    };
    Connection.prototype.close = function (code, reason) {
        this.webSocket.close(code, reason);
    };
    return Connection;
}());
exports.Connection = Connection;
exports.connection = Connection;
var WorkflowClient = /** @class */ (function () {
    function WorkflowClient(urlOrPath, options) {
        var _this = this;
        options = options || {};
        options.contextHeader = options.contextHeader || (function () { return window.location.hash.replace("#", ""); });
        options.contextHeaderEnabled = options.contextHeaderEnabled === undefined ? true : options.contextHeaderEnabled;
        this.connection = new Connection(urlOrPath);
        this.sessionId = options.sessionId || this.generateSessionId();
        this.senderId = options.senderId || "WebClient";
        this.handlers = [];
        this.handlersWorkflowsEvents = [];
        this.options = options;
        this.connection.opening(function () {
            _this.dispatchWorkflowsEventsMessage(new OpeningMessage(), _this);
        });
        this.connection.reopening(function () {
            _this.dispatchWorkflowsEventsMessage(new ReopeningMessage(_this.connection.connectionId), _this);
        });
        this.connection.closed(function () {
            _this.dispatchWorkflowsEventsMessage(new ClosedMessage(_this.connection.connectionId), _this);
        });
        this.connection.stateChanged(function (state) {
            _this.dispatchWorkflowsEventsMessage(new StateChangedMessage(state), _this);
        });
        this.connection.recieved(function (data) {
            _this.dispatchMessage(data, _this);
        });
        this.connection.sent(function (data) {
            _this.dispatchWorkflowsEventsMessage(new SentMessage(data), _this);
        });
        this.connection.error(function (error) {
            _this.dispatchWorkflowsEventsMessage(new ErrorMessage(error), _this);
        });
    }
    WorkflowClient.prototype.generateSessionId = function () {
        var sessionId = readCookie("SocketCore.SessionId");
        if (!sessionId) {
            sessionId = guid();
            createCookie("SocketCore.SessionId", sessionId, 0);
        }
        return sessionId;
    };
    WorkflowClient.prototype.run = function (handler) {
        this.connection.opened(handler);
        this.connection.open();
    };
    WorkflowClient.prototype.send = function (channel, message) {
        var options = this.options;
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
    };
    WorkflowClient.prototype.subscribe = function (fn) {
        this.handlers.push(fn);
    };
    WorkflowClient.prototype.unsubscribe = function (fn) {
        this.handlers = this.handlers.filter(function (item) {
            if (item !== fn) {
                return item;
            }
        });
    };
    WorkflowClient.prototype.dispatchMessage = function (data, thisObj) {
        this.handlers.forEach(function (handler) {
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
    };
    WorkflowClient.prototype.subscribeWorkflowsEventsChannel = function (fn) {
        this.handlersWorkflowsEvents.push(fn);
    };
    WorkflowClient.prototype.unsubscribeWorkflowsEventsChannel = function (fn) {
        this.handlersWorkflowsEvents = this.handlersWorkflowsEvents.filter(function (item) {
            if (item !== fn) {
                return item;
            }
        });
    };
    WorkflowClient.prototype.dispatchWorkflowsEventsMessage = function (msg, thisObj) {
        this.handlersWorkflowsEvents.forEach(function (item) {
            var message = clone(msg); // copy of the message
            item.call(thisObj, message);
        });
    };
    return WorkflowClient;
}());
exports.WorkflowClient = WorkflowClient;
exports.workflowClient = WorkflowClient;
var Message = /** @class */ (function () {
    function Message(ns, type, data, headers) {
        if (data === void 0) { data = null; }
        if (headers === void 0) { headers = null; }
        this.messageId = null;
        this.connectionId = null;
        this.sessionId = null;
        this.senderId = null;
        this.replyToMessageId = null;
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
    Message.prototype.isMatch = function (ns, type) {
        return this.namespace === ns && this.type === type;
    };
    return Message;
}());
exports.Message = Message;
exports.message = Message;
var MessageHeader = /** @class */ (function () {
    function MessageHeader(name, value) {
        this.name = name;
        this.value = value;
    }
    return MessageHeader;
}());
exports.MessageHeader = MessageHeader;
exports.messageHeader = MessageHeader;
var OpeningMessage = /** @class */ (function (_super) {
    __extends(OpeningMessage, _super);
    function OpeningMessage() {
        return _super.call(this, "SocketCore.WokrflowEvents", "Opening") || this;
    }
    return OpeningMessage;
}(Message));
exports.OpeningMessage = OpeningMessage;
var ReopeningMessage = /** @class */ (function (_super) {
    __extends(ReopeningMessage, _super);
    function ReopeningMessage(connectionId) {
        return _super.call(this, "SocketCore.WokrflowEvents", "Reopening", connectionId) || this;
    }
    return ReopeningMessage;
}(Message));
exports.ReopeningMessage = ReopeningMessage;
var ClosedMessage = /** @class */ (function (_super) {
    __extends(ClosedMessage, _super);
    function ClosedMessage(connectionId) {
        return _super.call(this, "SocketCore.WokrflowEvents", "Closed", connectionId) || this;
    }
    return ClosedMessage;
}(Message));
exports.ClosedMessage = ClosedMessage;
var StateChangedMessage = /** @class */ (function (_super) {
    __extends(StateChangedMessage, _super);
    function StateChangedMessage(state) {
        return _super.call(this, "SocketCore.WokrflowEvents", "StateChanged", state) || this;
    }
    return StateChangedMessage;
}(Message));
exports.StateChangedMessage = StateChangedMessage;
var SentMessage = /** @class */ (function (_super) {
    __extends(SentMessage, _super);
    function SentMessage(data) {
        return _super.call(this, "SocketCore.WokrflowEvents", "Sent", data) || this;
    }
    return SentMessage;
}(Message));
exports.SentMessage = SentMessage;
var ErrorMessage = /** @class */ (function (_super) {
    __extends(ErrorMessage, _super);
    function ErrorMessage(error) {
        return _super.call(this, "SocketCore.WokrflowEvents", "Error", error) || this;
    }
    return ErrorMessage;
}(Message));
exports.ErrorMessage = ErrorMessage;
