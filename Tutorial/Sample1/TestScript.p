module Test0
private START, CANCEL;
{
    Main, CoffeeMachine, Timer
}

module Test1 = (hide START, CANCEL in Test0);

test Test0: Test0;

test Test1: Test1;