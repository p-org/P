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

/* Test 0: To check that the simple (without fault-tolerance) 2PC protocol is safe in the absence of failure */
module ClientWithTimer = (rename Timer to Timer1 in (hide eTimeOut, eCancelSuccess, eCancelFailure, eStartTimer, eCancelTimer in (compose Client, Timer)));
module TwoPCWithTimer = (rename Timer to Timer2 in (hide eTimeOut, eCancelSuccess, eCancelFailure, eStartTimer, eCancelTimer in (compose TwoPC, Timer)));

test Test0: (rename TestDriver1 to Main in (compose TwoPCWithTimer, ClientWithTimer, Test0));

/* Test 1: To check that the simple (without fault-tolerance) 2PC protocol satisfies the AtomicitySpec in the absence of failure */
module TwoPCwithSpec = (assert AtomicitySpec in TwoPCWithTimer);
test Test1: (rename TestDriver1 to Main in (compose TwoPCwithSpec, ClientWithTimer, Test0));

