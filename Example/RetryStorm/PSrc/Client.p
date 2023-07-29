machine Client {
    var maxQPS: int;
    var server: Server;
    var time: int;
    var maxTime: int;
    var kClientAmplification: int;
    var clientAmplificationOffset: int;
    var currentRequestId: int;
    var waitingIds: set[int];
    start state Init {
        entry (payload: (maxQPS: int, server: Server, maxTime: int, kClientAmplification: int, clientAmplificationOffset: int)) {
            maxQPS = payload.maxQPS;
            server = payload.server;
            maxTime = payload.maxTime;
            kClientAmplification = payload.kClientAmplification;
            clientAmplificationOffset = payload.clientAmplificationOffset;
            currentRequestId = 0;
            time = 0;
        }
        on eStart goto Run with {
            send this, eClientRun, delay "0.1";
        }
    }
    state Run {
        on eClientRun do {
            var i: int;
            var _eRequestPayload: (id: int, client: Client);
            var nRequests: int;
            if (time == maxTime + 1) {
                send server, halt;
                raise halt;
            }
            if (time < maxTime) {
                i = 0;
                while (i < sizeof(waitingIds)) {
                    _eRequestPayload.id = waitingIds[i];
                    _eRequestPayload.client = this;
                    send server, eRequest, _eRequestPayload;
                    i = i + 1;
                }
                i = 0;
                if (time == clientAmplificationOffset) {
                    nRequests = maxQPS * kClientAmplification;
                }
                else {
                    nRequests = maxQPS;
                }
                while (i < nRequests) {
                    _eRequestPayload.id = currentRequestId;
                    _eRequestPayload.client = this;
                    send server, eRequest, _eRequestPayload;
                    waitingIds += (currentRequestId);
                    currentRequestId = currentRequestId + 1;
                    i = i + 1;
                }
            }
            send this, eClientRun;
            time = time + 1;
        }
        on eResponse do (payload: (id: int)) {
            waitingIds -= (payload.id);
        }
    }
}