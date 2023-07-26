machine Client {
    var maxQPS: int;
    var server: Server;
    var currentRequestId: int;
    var waitingIds: set[int];
    start state Init {
        entry (payload: (maxQPS: int, server: Server)) {
            maxQPS = payload.maxQPS;
            server = payload.server;
            currentRequestId = 0;
        }
        on eStart goto Run with {
            send this, eClientRun, delay "0.1";
        }
    }
    state Run {
        on eClientRun do {
            var i: int;
            var _eRequestPayload: (id: int, client: Client);
            i = 0;
            while (i < sizeof(waitingIds)) {
                _eRequestPayload.id = waitingIds[i];
                _eRequestPayload.client = this;
                send server, eRequest, _eRequestPayload;
                i = i + 1;
            }
            i = 0;
            while (i < maxQPS) {
                _eRequestPayload.id = currentRequestId;
                _eRequestPayload.client = this;
                send server, eRequest, _eRequestPayload;
                waitingIds += (currentRequestId);
                currentRequestId = currentRequestId + 1;
                i = i + 1;
            }
            send this, eClientRun;
        }
        on eResponse do (payload: (id: int)) {
            waitingIds -= (payload.id);
        }
    }
}