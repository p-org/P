machine Server {
    var maxQPS: int;
    var currentQPS: int;
    var servedRequestIds: set[int];
    start state Init {
        entry (payload: (maxQPS: int)) {
            maxQPS = payload.maxQPS;
            currentQPS = 0;
        }
        on eStart goto Wait with {
            send this, eServerRun, delay "0.2";
        }
    }
    state Wait {
        defer eRequest;
        on eServerRun goto Run with {
            currentQPS = 0;
            send this, eServerRun;
       }
    }
    state Run {
        defer eServerRun;
        on eRequest do (payload: (id: int, client: Client)) {
            var _eResponsePayload: (id: int);
            if (!(payload.id in servedRequestIds)) {
                _eResponsePayload.id = payload.id;
                send payload.client, eResponse, _eResponsePayload;
                servedRequestIds += (payload.id);
            }
            currentQPS = currentQPS + 1;
            if (currentQPS == maxQPS) {
                goto Wait;
            }
        }
        on null goto Wait;
    }
}
