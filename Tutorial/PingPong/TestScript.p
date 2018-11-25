module System = 
{
    ClientMachine, ServerMachine, Timer
};

test Test0[main = Test_1_Machine]: (assert Safety in (compose { Test_1_Machine }, System));

test Test1[main = Test_2_Machine]: (assert Liveness in (compose { Test_2_Machine }, System));
