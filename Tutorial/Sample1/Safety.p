spec Safety observes eDoorOpened, eDoorClosed, mMachineBusy { 
    var open: bool;
    start state Init { 
        on eDoorOpened do { 
            open = true;
        }
        on eDoorClosed do {
            open = false;
        }
        on mMachineBusy do {
            assert(!open);
        }
    }
} 
