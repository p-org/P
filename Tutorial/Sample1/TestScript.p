module CoffeeMaker
private START, CANCEL;
{
    CoffeeMachine, Timer
}

module Main0 { Main0 }

module Main1 { Main1 }

module Test0 = (compose Main0, CoffeeMaker);

module Test1 = (compose Main1, CoffeeMaker);

test Test0: (rename Main0 to Main in Test0);

test Test1: (rename Main1 to Main in Test1);