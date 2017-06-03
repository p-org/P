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

module TestDriver1 {
    TestDriver1
}

module TestDriver2 {
    TestDriver2
}


module ClientWithTimer = (rename Timer to Timer1 in 
        (hide eTimeOut, eCancelSuccess, eCancelFailure, eStartTimer, eCancelTimer in (compose Client, Timer)));
module NoFault_TwoPCWithTimer = (rename Timer to Timer2 in 
        (hide eTimeOut, eCancelSuccess, eCancelFailure, eStartTimer, eCancelTimer in (compose TwoPC, Timer)));
module FaultTolerant_TwoPCWithTimer = (rename Timer to Timer2 in 
        (hide eTimeOut, eCancelSuccess, eCancelFailure, eStartTimer, eCancelTimer in (compose TwoPC, Timer)));

// Test 0: To check that the simple (without fault-tolerance) 2PC protocol is safe in the absence of failure 
test Test0: (rename TestDriver1 to Main in (compose NoFault_TwoPCWithTimer, ClientWithTimer, TestDriver1));

// Test 1: To check that the simple (without fault-tolerance) 2PC protocol satisfies the AtomicitySpec in the absence of failure 
module TwoPCwithSpec = (assert AtomicitySpec in NoFault_TwoPCWithTimer);
test Test1: (rename TestDriver1 to Main in (compose TwoPCwithSpec, ClientWithTimer, TestDriver1));


module LinearAbs {
    LinearizibilityAbs
}

/* Test 0: To check that the fault tolerant 2PC protocol is safe 
test Test2: (rename TestDriver1 to Main in (compose FaultTolerant_TwoPCWithTimer, LinearAbs, ClientWithTimer, TestDriver2));

/* Test 1: To check that the fault tolerant 2PC protocol satisfies the AtomicitySpec 
module TwoPCwithSpec_FT = (assert AtomicitySpec in FaultTolerant_TwoPCWithTimer);
test Test3: (rename TestDriver1 to Main in (compose TwoPCwithSpec_FT, LinearAbs, ClientWithTimer, TestDriver2));


*/
