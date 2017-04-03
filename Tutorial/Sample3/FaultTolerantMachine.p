machine FaultTolerantMachine {
    start state Init {
        
    }
}

machine ReliableStorage {
    var i: int;
    var done: bool;

    start state Init {
        entry {
            i = 0;
            done = false;
        }
        on DoOp do {
            if (!done) {
                i = i + 1;
                done = true;
            }
        }
    }
}