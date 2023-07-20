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

        journal = new Journal();
        send journal, eStart;

        _StoragePayload.initT = 0.3;
        _StoragePayload.period = 1.0;
        _StoragePayload.key = "key1";
        _StoragePayload.s_id = "s_id1";
        _StoragePayload.journal = journal;
        storages[_StoragePayload.key] = new Storage(_StoragePayload);
        send storages[_StoragePayload.key], eStart;

        _WriterPayload.initT = 0.1;
        _WriterPayload.period = 1.0;
        _WriterPayload.journal = journal;
        _WriterPayload.storages = storages;
        _WriterPayload.rho = 1.0;
        _WriterPayload.backPressure = true;
        writer = new Writer(_WriterPayload);
        send writer, eStart;

        _ReaderPayload.initT = 0.2;
        _ReaderPayload.period = 1.0;
        _ReaderPayload.storages = storages;
        reader = new Reader(_ReaderPayload);
        send reader, eStart;
    }
  }
}

test temp [main = TestCase]:
  assert Spec in (union Module, { TestCase });
