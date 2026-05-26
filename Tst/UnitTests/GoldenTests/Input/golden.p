// Input program for the golden code-generation snapshot tests. Kept small but exercises a
// spread of expressions and statements (arithmetic, if/else, seq ops, sizeof, print, goto)
// plus a spec monitor, so the snapshots are meaningful across PChecker/PEx/PObserve.
event eReq: int;
event eResp: int;

machine GoldenMachine {
    var count: int;
    var log: seq[int];

    start state Init {
        entry {
            count = 0;
            goto Serving;
        }
    }

    state Serving {
        on eReq do (n: int) {
            count = count + n;
            log += (sizeof(log), n);
            if (count > 10) {
                count = 0;
            } else {
                print format("count={0}", count);
            }
        }
    }
}

spec GoldenMonitor observes eReq {
    var total: int;

    start state Counting {
        on eReq do (n: int) {
            total = total + n;
            assert total >= 0, "total must stay non-negative";
        }
    }
}
