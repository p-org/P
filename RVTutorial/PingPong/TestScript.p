module System = 
{
    ClientMachine, ServerMachine, Timer
};

test Test0: main Test_1_Machine in (assert Safety in (union { Test_1_Machine }, System));

test Test1: main Test_2_Machine in (assert Liveness in (union { Test_2_Machine }, System));
