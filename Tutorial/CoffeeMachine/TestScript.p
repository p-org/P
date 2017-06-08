module CoffeeMaker
{
    CoffeeMachineController, CoffeeMachine, Timer
}

module Main0 { EspressoButton, Main0 }
module Test0 = (compose Main0, CoffeeMaker);
test Test0: (rename Main0 to Main in Test0);

module Main1 { SteamerButton, Main1 }
module Test1 = (compose Main1, CoffeeMaker);
test Test1: (rename Main1 to Main in Test1);

module Main2 { Door, Main2 }
module Test2 = (compose Main2, CoffeeMaker);
test Test2: (rename Main2 to Main in Test2);