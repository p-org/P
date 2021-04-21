event eGetReq: tGetReq;
event eGetResp: tGetResp;
event ePutReq: tPutReq;
event ePutResp: tPutResp;
// client is request sender, rid is the unique request Id
type tGetReq = (key:string, rId: int);
// res is true if the key is found and false if not
type tGetResp = (res: bool, record: tRecord, rId: int);
type tPutReq = (record: tRecord, rId: int);
type tPutResp = (res: bool, record: tRecord, rId: int);
/*sqr is a monotonically increasing time stamp value*/
type tRecord = (key: string, val: int, sqr: int);


spec getConsistency observes eGetResp, eGetReq, ePutResp, ePutReq {
    // map from get request id (int) to the snapshot of the record when the get request was issued.
    var snapshotAtGetReq: map[int, tRecord];
    // map from key to the record corresponding to the last successfully responded put for that key
    var putResponseMap: map[string, tRecord];

    start state WaitForGetAndPutOperations {
        on ePutReq do DoNothing;
        on ePutResp do UpdatePutResponseMap;
        on eGetReq do CreateSnapshotForGetReq;
        on eGetResp do CheckGetRespConsistency;
    }

    fun DoNothing() {
    }

    fun UpdatePutResponseMap(putResp: tPutResp) {
        if (putResp.res) {
            // update the latest record value for the key
            putResponseMap[putResp.record.key] = putResp.record;
        }
    }

    fun CreateSnapshotForGetReq(getReq: tGetReq) {
        if (getReq.key in putResponseMap) {
            snapshotAtGetReq[getReq.rId] = putResponseMap[getReq.key];
        } else {
            snapshotAtGetReq[getReq.rId] = GetEmptyRecord(getReq.key);
        }
    }

    fun CheckGetRespConsistency(getResp: tGetResp) {
        var getRespRecord: tRecord;
        getRespRecord = getResp.record;

        if (!getResp.res) {
            assert (GetEmptyRecord(getRespRecord.key) == snapshotAtGetReq[getResp.rId]),
                format ("Get is not Consistent!! Get responded KEYNOTFOUND for a key '{{{0}}}', even when a record {0} existed", getRespRecord.key);
        } else {
            assert (snapshotAtGetReq[getResp.rId].sqr <= getRespRecord.sqr),
                format ("For key {0}, expected value of sequencer is >= {1} but got {2}. Get is not Consistent!", getRespRecord.key, snapshotAtGetReq[getResp.rId].sqr, getRespRecord.sqr);
            // remove the snapshot of the sqr for the key
            snapshotAtGetReq -= (getResp.rId);
        }
    }

    fun GetEmptyRecord(key: string): tRecord {
        return (key = key, val = -1, sqr = -1);
    }
}
