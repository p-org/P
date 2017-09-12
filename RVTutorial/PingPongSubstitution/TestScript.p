module System = 
{
    IClient -> Client, IServer -> Server, Timer
};

test Test0: main TestMachine0 in (assert Safety in (union { TestMachine0 }, System));

test Test1: main TestMachine1 in (assert Liveness in (union { TestMachine1 }, System));
