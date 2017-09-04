module System = 
{
    IClient -> Client, IServer -> Server, Timer
};

test Test0: (rename TestMachine0 to Main in (assert Safety in (compose { TestMachine0 }, System)));

test Test1: (rename TestMachine1 to Main in (assert Liveness in (compose { TestMachine1 }, System)));
