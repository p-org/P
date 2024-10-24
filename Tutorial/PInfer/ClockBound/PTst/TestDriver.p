fun SetUpSystem(num_clients: int, req_per_client: int) {
    var i: int;
    var clock: GlobalClock;
    i = 0;
    clock = new GlobalClock();
    while (i < num_clients) {
        new Client((globalClock=clock, n=req_per_client));
        i = i + 1;
    }
}

machine C1R3 {
    start state Init {
        entry {
            SetUpSystem(1, 3);
        }
    }
}

machine C2R3 {
    start state Init {
        entry {
            SetUpSystem(2, 3);
        }
    }
}

machine C3R3 {
    start state Init {
        entry {
            SetUpSystem(3, 3);
        }
    }
}

machine C4R3 {
    start state Init {
        entry {
            SetUpSystem(4, 3);
        }
    }
}

machine C3R5 {
    start state Init {
        entry {
            SetUpSystem(3, 5);
        }
    }
}

test tcC1R3 [main = C1R3]:
    assert Correctness in (union { C1R3 }, ClockBound);

test tcC2R3 [main = C2R3]:
    assert Correctness in (union { C2R3 }, ClockBound);

test tcC3R3 [main = C3R3]:
    assert Correctness in (union { C3R3 }, ClockBound);

test tcC4R3 [main = C4R3]:
    assert Correctness in (union { C4R3 }, ClockBound);

test tcC3R5 [main = C3R5]:
    assert Correctness in (union { C3R5 }, ClockBound);