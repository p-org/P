module System
private START, TIMEOUT;
{
    Client, Server, Timer
}

module Main0 { TestMachine0 }

module Main1 { TestMachine1 }

test Test0: (rename TestMachine0 to Main in (compose Main0, System));

test Test1: (rename TestMachine1 to Main in (compose Main1, System));

