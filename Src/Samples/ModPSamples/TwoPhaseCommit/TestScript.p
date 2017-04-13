module TwoPC {
    Coordinator, 
    Participant
}

module Client {
    ClientMachine
}

module Timer {
    Timer
}

module Test0 {
    TestDriver1
}

module ClientWithTimer = (rename Timer to Timer1 in (hide eTimeOut, eCancelSuccess, eCancelFailure, eStartTimer, eCancelTimer in (compose Client, Timer)));
module TwoPCWithTimer = (rename Timer to Timer2 in (hide eTimeOut, eCancelSuccess, eCancelFailure, eStartTimer, eCancelTimer in (compose TwoPC, Timer)));

test Test0: (rename TestDriver1 to Main in (compose TwoPCWithTimer, ClientWithTimer, Test0));