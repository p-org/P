fun Temp(rho: float): float;

type Subscription = (t: float, key: string, queue: set[float]);

event eStart;
event eReaderRun: (t: float);
event eWriterRun: (t: float);
event eStorageRun: (t: float);
event ePollRequest: (s_id: string, storage: Storage);
event ePollResponse: (t: float);
event eEnqueueRequest: (key: string, t: float);
event eSubscribeRequest: (s_id: string, key: string);
event eReadRequest: (t: float, reader: Reader);
event eReadResponse: (tStart: float, tEnd:float);
event eAtTRequest: (writer: Writer);
event eAtTResponse: (t: float);

delay eStart: "0.0": false;
delay eReaderRun: "0.0": false;
delay eWriterRun: "0.0": false;
delay eStorageRun: "0.0": false;
delay ePollRequest: "0.0": false;
delay ePollResponse: "0.0": false;
delay eEnqueueRequest: "0.0": false;
delay eSubscribeRequest: "0.0": false;
delay eReadRequest: "0.0": false;
delay eReadResponse: "0.0": false;

machine Journal {
    var subs: map[string, Subscription];

    start state Init {
        on eStart goto Run;
    }

    state Run {
        on eEnqueueRequest do (payload: (key: string, t: float)) {
            var s_id: string;
            foreach (s_id in keys(subs)) {
                if (subs[s_id].key == payload.key) {
                    subs[s_id].queue += (payload.t);
                }
            }
        }
        on eSubscribeRequest do (payload: (s_id: string, key: string)) {
            var subscription: Subscription;
            subscription.t = 0.0;
            subscription.key = payload.key;
            subs[payload.s_id] = subscription;
        }
        on ePollRequest do (payload: (s_id: string, storage: Storage)) {
            var _ePollResponsePayload: (t: float);
            var t: float;
            var tMin: float;
            _ePollResponsePayload.t = -1.0;
            if (sizeof(subs[payload.s_id].queue) > 0) {
                tMin = subs[payload.s_id].queue[0];
                foreach (t in subs[payload.s_id].queue) {
                    if (t < tMin) {
                        tMin = t;
                    }
                }
                subs[payload.s_id].queue -= (tMin);
                _ePollResponsePayload.t = tMin;
            }
            send payload.storage, ePollResponse, _ePollResponsePayload;
        }
    }
}

machine Storage {
    var initT: float;
    var period: float;
    var journal: Journal;
    var s_id: string;
    var at_t: float;
    var blockedReads: set[(t: float, reader: Reader)];
    var eStorageRunT: float;

    start state Init {
        entry (payload: (initT: float, period: float, key: string, s_id: string, journal: Journal)) {
            var _eSubscribeRequestPayload: (s_id: string, key: string);
            initT = payload.initT;
            period = payload.period;
            journal = payload.journal;
            s_id = payload.s_id;
            at_t = 0.0;
            _eSubscribeRequestPayload.s_id = s_id;
            _eSubscribeRequestPayload.key = payload.key;
            send journal, eSubscribeRequest, _eSubscribeRequestPayload;
        }
        on eStart goto Run with {
            var _eStorageRunPayload: (t: float);
            _eStorageRunPayload.t = initT;
            send this, eStorageRun, _eStorageRunPayload, delay format("{0}", initT);
        }
    }

    state Run {
        on eStorageRun do (payload: (t: float)) {
            var next_t: float;
            var _ePollRequestPayload: (s_id: string, storage: Storage);
            var readRequestPayload: (t: float, reader: Reader);
            var _eReadResponsePayload: (tStart: float, tEnd:float);
            var _eStorageRunPayload: (t: float);
            var stillBlockedReads: set[(t: float, reader: Reader)];
            _ePollRequestPayload.s_id = s_id;
            _ePollRequestPayload.storage = this;
            send journal, ePollRequest, _ePollRequestPayload;
            receive {
                case ePollResponse: (payload: (t: float)) {
                    next_t = payload.t;
                }
            }
            if (next_t != -1.0) {
                at_t = next_t;
            } else {
                at_t = payload.t;
            }
            foreach (readRequestPayload in blockedReads) {
                if (readRequestPayload.t > at_t) {
                    stillBlockedReads += (readRequestPayload);
                } else {
                    _eReadResponsePayload.tStart = readRequestPayload.t;
                    _eReadResponsePayload.tEnd = payload.t;
                    send readRequestPayload.reader, eReadResponse, _eReadResponsePayload;
                }
            }
            blockedReads = stillBlockedReads;
            _eStorageRunPayload.t = payload.t + period;
            send this, eStorageRun, _eStorageRunPayload, delay format("{0}", period); // This should be a delayed event
        }
        on eReadRequest do (payload: (t: float, reader: Reader)) {
            blockedReads += (payload);
        }
        on eAtTRequest do (payload: (writer: Writer)) {
            var _eAtTResponsePayload: (t: float);
            _eAtTResponsePayload.t = at_t;
            send payload.writer, eAtTResponse, _eAtTResponsePayload;
        }
    }

    /*state Run {
        on eStorageRun do (payload: (t: float)) {
            var _ePollRequestPayload: (s_id: string, storage: Storage);
            _ePollRequestPayload.s_id = s_id;
            _ePollRequestPayload.storage = this;
            eStorageRunT = payload.t;
            send journal, ePollRequest, _ePollRequestPayload;
        }
        on eReadRequest do (payload: (t: float, reader: Reader)) {
            blockedReads += (payload);
        }
        on ePollResponse do (payload: (t: float)) {
            var next_t: float;
            var readRequestPayload: (t: float, reader: Reader);
            var _eReadResponsePayload: (tStart: float, tEnd:float);
            var _eStorageRunPayload: (t: float);
            var stillBlockedReads: set[(t: float, reader: Reader)];
            next_t = payload.t;
            if (next_t != -1.0) {
                at_t = next_t;
            } else {
                at_t = eStorageRunT;
            }
            foreach (readRequestPayload in blockedReads) {
                if (readRequestPayload.t > at_t) {
                    stillBlockedReads += (readRequestPayload);
                } else {
                    _eReadResponsePayload.tStart = readRequestPayload.t;
                    _eReadResponsePayload.tEnd = eStorageRunT;
                    send readRequestPayload.reader, eReadResponse, _eReadResponsePayload;
                }
            }
            blockedReads = stillBlockedReads;
            _eStorageRunPayload.t = eStorageRunT + period;
            send this, eStorageRun, _eStorageRunPayload, delay format("{0}", period); // This should be a delayed event
        }
        on eAtTRequest do (payload: (writer: Writer)) {
            var _eAtTResponsePayload: (t: float);
            _eAtTResponsePayload.t = at_t;
            send payload.writer, eAtTResponse, _eAtTResponsePayload;
        }
    }*/
}

machine Writer {
    var initT: float;
    var period: float;
    var journal: Journal;
    var backPressure: bool;
    var storages: map[string, Storage];
    var rho: float;

    start state Init {
        entry (payload: (initT: float, period: float, journal: Journal, storages: map[string, Storage], rho:float, backPressure: bool)) {
            initT = payload.initT;
            period = payload.period;
            journal = payload.journal;
            rho = payload.rho;
            backPressure = payload.backPressure;
            storages = payload.storages;
        }
        on eStart goto Run with {
            var _eWriterRunPayload: (t: float);
            _eWriterRunPayload.t = initT;
            send this, eWriterRun, _eWriterRunPayload, delay format("{0}", initT);
        }
    }

    state Run {
        on eWriterRun do (payload: (t: float)) {
            var _eEnqueueRequestPayload: (key: string, t: float);
            var _eWriterRunPayload: (t: float);
            var _eAtTRequestPayload: (writer: Writer);
            var lag: float;
            var at_t: float;
            _eEnqueueRequestPayload.key = choose(keys(storages));
            _eEnqueueRequestPayload.t = payload.t;
            send journal, eEnqueueRequest, _eEnqueueRequestPayload;
            period = Temp(rho);
            if (backPressure) {
                _eAtTRequestPayload.writer = this;
                send storages[_eEnqueueRequestPayload.key], eAtTRequest, _eAtTRequestPayload;
                receive {
                    case eAtTResponse: (payload: (t: float)) {
                        at_t = payload.t;
                    }
                }
                lag = payload.t - at_t;
                if (lag > 1.0) {
                    period = period + lag - 1.0;
                }
            }
            _eWriterRunPayload.t = payload.t + period;
            send this, eWriterRun, _eWriterRunPayload, delay format("{0}", period); // This should be a delayed event with backpressure delay
        }
    }
}

machine Reader {
    var initT: float;
    var period: float;
    var storages: map[string, Storage];

    start state Init {
        entry (payload: (initT: float, period: float, storages: map[string, Storage])) {
            initT = payload.initT;
            period = payload.period;
            storages = payload.storages;
        }
        on eStart goto Run with {
            var _eReaderRunPayload: (t: float);
            _eReaderRunPayload.t = initT;
            send this, eReaderRun, _eReaderRunPayload, delay format("{0}", initT);
        }
    }

    state Run {
        on eReaderRun do (payload: (t: float)) {
            var _eReadRequestPayload: (t: float, reader: Reader);
            var _eReaderRunPayload: (t: float);
            _eReadRequestPayload.t = payload.t;
            _eReadRequestPayload.reader = this;
            send storages[choose(keys(storages))], eReadRequest, _eReadRequestPayload;
            _eReaderRunPayload.t = payload.t + period;
            send this, eReaderRun, _eReaderRunPayload, delay format("{0}", period); // This should be a delayed event
        }
        on eReadResponse do (payload: (tStart: float, tEnd: float)) {
            var tStart: float;
            var tEnd: float;
            tStart = payload.tStart;
            tEnd = payload.tEnd;
        }
    }
}

module Module = { Journal, Storage, Reader, Writer };
