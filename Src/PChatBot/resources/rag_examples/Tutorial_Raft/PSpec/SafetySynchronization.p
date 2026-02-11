spec SafetySynchronization observes eClientPutRequest, eClientGetRequest, eRaftGetResponse, eRaftPutResponse, eBecomeLeader {
    var localKVStore: KVStore;
    var requestResultMap: map[Client, map[int, Result]];
    var getRequestMap: map[Client, map[TransId, KeyT]];
    var putRequestMap: map[Client, map[TransId, (key: KeyT, value: ValueT)]];
    var seenId: map[Client, set[int]];
    var respondedId : map[Client, set[int]];
    var currentLeader: Server;
    var term: TermId;

    start state Init {
        entry {
            localKVStore = newStore();
            requestResultMap = default(map[Client, map[int, Result]]);
            getRequestMap = default(map[Client, map[TransId, KeyT]]);
            putRequestMap = default(map[Client, map[TransId, (key: KeyT, value: ValueT)]]);
            seenId = default(map[Client, set[int]]);
            respondedId = default(map[Client, set[int]]);
            currentLeader = default(Server);
            term = -1;
            goto Listening;
        }
    }

    state Listening {
        on eBecomeLeader do (payload: tBecomeLeader) {
            if (payload.term > term) {
                currentLeader = payload.leader;
                term = payload.term;
            }
        }

        on eClientGetRequest do (payload: tClientGetRequest) {
            if (!(payload.client in keys(seenId))) {
                seenId[payload.client] = default(set[int]);
            }
            if (!(payload.client in keys(respondedId))) {
                respondedId[payload.client] = default(set[int]);
            }
            if (!(payload.client in keys(getRequestMap))) {
                getRequestMap[payload.client] = default(map[TransId, KeyT]);
            }
            if (payload.client == payload.sender && !(payload.transId in seenId[payload.client])) {
                seenId[payload.client] += (payload.transId);
            }
            getRequestMap[payload.client][payload.transId] = payload.key;
        }

        on eClientPutRequest do (payload: tClientPutRequest) {
            if (!(payload.client in keys(seenId))) {
                seenId[payload.client] = default(set[int]);
            }
            if (!(payload.client in keys(respondedId))) {
                respondedId[payload.client] = default(set[int]);
            }
            if (!(payload.client in keys(putRequestMap))) {
                putRequestMap[payload.client] = default(map[TransId, (key: KeyT, value: ValueT)]);
            }
            if (payload.client == payload.sender && !(payload.transId in seenId[payload.client])) {
                seenId[payload.client] += (payload.transId);
            }
            putRequestMap[payload.client][payload.transId] = (key=payload.key, value=payload.value);
        }

        on eRaftGetResponse do (payload: tRaftGetResponse) {
            var execResult: ExecutionResult;
            checkResponseValid(payload.client, payload.sender, payload.transId);
            if (!(payload.client in keys(respondedId)) && currentLeader == payload.sender) {
                respondedId[payload.client] += (payload.transId);
                execResult = executeGet(localKVStore, getRequestMap[payload.client][payload.transId]);
                assert execResult.result.success == payload.success, format("Inconsistent status: {0}", getRequestMap[payload.client][payload.transId]);
                assert execResult.result.value == payload.value, format("Inconsistent Get result! Expected {0}, got {1}", execResult.result.value, payload.value);
            }
        }

        on eRaftPutResponse do (payload: tRaftPutResponse) {
            var execResult: ExecutionResult;
            checkResponseValid(payload.client, payload.sender, payload.transId);
            if (!(payload.client in keys(respondedId)) && currentLeader == payload.sender) {
                respondedId[payload.client] += (payload.transId);
                assert payload.client in keys(putRequestMap);
                execResult = executePut(localKVStore, putRequestMap[payload.client][payload.transId].key, putRequestMap[payload.client][payload.transId].value);
                localKVStore = execResult.newState;
            }
        }
    }
    fun checkResponseValid(client: Client, sender: Server, tId: TransId) {
        assert currentLeader != default(Server), "Leader is un-initialized but got an eRaftResponse!";
        if (currentLeader == sender) {
            assert client in keys(seenId), format("Responding to a client that has not sent any request: {0}", client);
            assert tId in seenId[client], format("Responding to a non-existing transactionId {0} sent by {1}", tId, client);
        }
    }
}
