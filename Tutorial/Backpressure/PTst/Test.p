machine TestCase {
  start state Init {
    entry {
        var journal: Journal;
        var storage: Storage;
        var writer: Writer;
        var reader: Reader;
        var storages: map[string, Storage];
        var _StoragePayload: (initT:float, period: float, key: string, s_id: string, journal: Journal);
        var _WriterPayload: (initT:float, period: float, journal: Journal, storages: map[string, Storage], rho: float, backPressure: bool);
        var _ReaderPayload: (initT:float, period: float, storages: map[string, Storage]);
        var _payload: (storages: map[string, Storage]);
        var shards: int;
        var i: int;

        shards = 2;

        journal = new Journal();
        send journal, eStart;

        i = 0;
        while (i < shards) {
            _StoragePayload.initT = Random();
            _StoragePayload.period = 1.0;
            _StoragePayload.key = format("key{0}", i);
            _StoragePayload.s_id = format("s_id{0}", i);
            _StoragePayload.journal = journal;
            storages[_StoragePayload.key] = new Storage(_StoragePayload);
            send storages[_StoragePayload.key], eStart;
            i = i + 1;
        }

        _WriterPayload.initT = Random();
        _WriterPayload.period = 1.0;
        _WriterPayload.journal = journal;
        _WriterPayload.storages = storages;
        _WriterPayload.rho = 1.0;
        _WriterPayload.backPressure = true;
        writer = new Writer(_WriterPayload);
        send writer, eStart;

        _ReaderPayload.initT = Random();
        _ReaderPayload.period = 1.0;
        _ReaderPayload.storages = storages;
        reader = new Reader(_ReaderPayload);
        send reader, eStart;
    }
  }
}

test temp [main = TestCase]:
  assert Spec in (union Module, { TestCase });
