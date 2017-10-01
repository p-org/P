// The module that implements the two phase commit protocol
module TwoPC = { CoorClientInterface -> Coordinator, SMRReplicatedMachineInterface -> Participant };

//The client module that interacts with the two phase commit protocol
module Client = { ClientInterface -> ClientMachine };


//The module that implements the linearizability abstraction for the SMR protocols
module LinearAbs = { SMRServerInterface -> LinearizabilityAbs };

// Client module composed with Timer module. Timer machine is made private in the new constructed module.
module ClientWithTimer = (rename ITimer to ITimer1 in
        //make the time interface private
        (hidei ITimer in
        //make all events of timer private
        (hidee eTimeOut, eCancelSuccess, eCancelFailure, eStartTimer, eCancelTimer in (compose Client, { ITimer -> Timer }))));

// Two phase commit protocol composed with the Timer module. Timer machine is made private in the new constructed module.
module TwoPCWithTimer = (rename ITimer to ITimer2 in 
        //make the time interface private
        (hidei ITimer in
        //make all events of timer private
        (hidee eTimeOut, eCancelSuccess, eCancelFailure, eStartTimer, eCancelTimer in (compose TwoPC, { ITimer -> Timer }))));


// Test 0: To check that the fault tolerant 2PC protocol is safe 
test Test0: main TestDriver2 in (compose TwoPCWithTimer, LinearAbs, ClientWithTimer, { TestDriver2 });

// Test 1: To check that the fault tolerant 2PC protocol satisfies the ConsistencySpec 
module TwoPCwithConsistencySpec = (assert ConsistencySpec in TwoPCWithTimer);
test Test1: main TestDriver2 in (compose TwoPCwithConsistencySpec, LinearAbs, ClientWithTimer, { TestDriver2 });

//Test 2: To check that the fault tolerant 2PC protocol satisfies the ProgressSpec
module TwoPCwithProgressSpec = (assert ProgressSpec in (compose TwoPCWithTimer, LinearAbs, ClientWithTimer, { TestDriver2 }));
test Test2: main TestDriver2 in TwoPCwithProgressSpec;

