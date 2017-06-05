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

module LinearAbs {
    LinearizibilityAbs
}

module ClientWithTimer = (rename Timer to Timer1 in 
        (hide eTimeOut, eCancelSuccess, eCancelFailure, eStartTimer, eCancelTimer in (compose Client, Timer)));
module TwoPCWithTimer = (rename Timer to Timer2 in 
        (hide eTimeOut, eCancelSuccess, eCancelFailure, eStartTimer, eCancelTimer in (compose TwoPC, Timer)));

// Test 0: To check that the simple (without fault-tolerance) 2PC protocol is safe in the absence of failure 
test Test0: (rename TestDriver1 to Main in (compose TwoPCWithTimer, LinearAbs, ClientWithTimer, TestDriver1));

// Test 1: To check that the simple (without fault-tolerance) 2PC protocol satisfies the AtomicitySpec in the absence of failure 
module TwoPCwithSpec = (assert AtomicitySpec in TwoPCWithTimer);
test Test1: (rename TestDriver1 to Main in (compose TwoPCwithSpec, LinearAbs, ClientWithTimer, TestDriver1));





// Test 2: To check that the fault tolerant 2PC protocol is safe 
test Test2: (rename TestDriver2 to Main in (compose TwoPCWithTimer, LinearAbs, ClientWithTimer, TestDriver2));

// Test 3: To check that the fault tolerant 2PC protocol satisfies the AtomicitySpec 
module TwoPCwithSpec_FT = (assert AtomicitySpec in TwoPCWithTimer);
test Test3: (rename TestDriver2 to Main in (compose TwoPCwithSpec_FT, LinearAbs, ClientWithTimer, TestDriver2));



