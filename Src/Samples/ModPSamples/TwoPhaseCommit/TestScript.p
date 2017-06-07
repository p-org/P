// The module that implements the two phase commit protocol
module TwoPC {
    Coordinator, 
    Participant
}

//The client module that interacts with the two phase commit protocol
module Client {
    ClientMachine
}

//The timer module that implements the OS timer
module Timer {
    Timer
}

//The test driver module that tests the two phase commit protocol without fault tolerance
module TestDriver1 {
    TestDriver1
}

//The test driver module that tests the two phase commit protocol with fault tolerance
module TestDriver2 {
    TestDriver2
}

//The module that implements the linearizability abstraction for the SMR protocols
module LinearAbs {
    LinearizabilityAbs
}

// Client module composed with Timer module. Timer machine is made private in the new constructed module.
module ClientWithTimer = (rename Timer to Timer1 in 
        (hide eTimeOut, eCancelSuccess, eCancelFailure, eStartTimer, eCancelTimer in (compose Client, Timer)));

// Two phase commit protocol composed with the Timer module. Timer machine is made private in the new constructed module.
module TwoPCWithTimer = (rename Timer to Timer2 in 
        (hide eTimeOut, eCancelSuccess, eCancelFailure, eStartTimer, eCancelTimer in (compose TwoPC, Timer)));

// Test 0: To check that the 2PC protocol (without fault-tolerance)  is safe. 
test Test0: (rename TestDriver1 to Main in (compose TwoPCWithTimer, LinearAbs, ClientWithTimer, TestDriver1));

// Test 1: To check that the 2PC protocol (without fault-tolerance) satisfies the AtomicitySpec. 
module TwoPCwithSpec = (assert AtomicitySpec in TwoPCWithTimer);
test Test1: (rename TestDriver1 to Main in (compose TwoPCwithSpec, LinearAbs, ClientWithTimer, TestDriver1));





// Test 2: To check that the fault tolerant 2PC protocol is safe 
test Test2: (rename TestDriver2 to Main in (compose TwoPCWithTimer, LinearAbs, ClientWithTimer, TestDriver2));

// Test 3: To check that the fault tolerant 2PC protocol satisfies the AtomicitySpec 
module TwoPCwithSpecFT = (assert AtomicitySpec in TwoPCWithTimer);
test Test3: (rename TestDriver2 to Main in (compose TwoPCwithSpecFT, LinearAbs, ClientWithTimer, TestDriver2));



