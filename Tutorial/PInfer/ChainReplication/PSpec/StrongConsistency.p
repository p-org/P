/* StrongConsistency.p : specification of strong consistency semantics from the clinet side
*/
spec StrongConsistency observes eReadSuccess, eReadFail, eWriteResponse {
    var storeState : map[tKey, tValue];
    var sequenceIds: map[tKey, tSequencer];
    start state Init {
        entry { goto WaitForEvents; }
    }
    state WaitForEvents {
        on eWriteResponse do (resp: (k: tKey, v: tValue, sequence_id: tSequencer)) {
            var s: tSequencer;
            print format ("update in storeState");
            if (!(resp.k in sequenceIds)) {
                // assert resp.sequence_id == 0,
                //     format("First write for key {0} should have sequence_id 0, but got {1}.",
                //         resp.k, resp.sequence_id);
                sequenceIds[resp.k] = resp.sequence_id;
            } else {
                s = sequenceIds[resp.k];
                assert resp.sequence_id >= s, format("Sequence_id mismatch for key {0}. Expected = {1}, actual = {2}.",
                resp.k, sequenceIds[resp.k] + 1, resp.sequence_id);
            }
            sequenceIds[resp.k] = resp.sequence_id;
            storeState[resp.k] = resp.v;
        }
        on eReadSuccess do (resp: (target: machine, k: tKey, v: tValue, sequence_id: tSequencer)) {
            print format ("storeState = {0}", storeState);
            assert resp.k in storeState,
                format("READ result mismatch for key {0}. Expected = {1}, actual = {2},",
                    resp.k, storeState[resp.k], resp.v);
            print format ("resp.k {0} in storeState", resp.k);
            assert resp.v == storeState[resp.k],
                format("READ result mismatch for key {0}. Expected = {1}, actual = {2},",
                    resp.k, storeState[resp.k], resp.v);
        }

        on eReadFail do (resp: (k: tKey)) {
            print format ("storeState = {0}", storeState);
            assert !(resp.k in storeState),
                format("READ request should fail for key {0}.", resp.k);
        }
    }
}

