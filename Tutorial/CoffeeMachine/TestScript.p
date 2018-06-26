module CoffeeMaker = { CoffeeMakerControllerMachine, CoffeeMakerMachine, Timer };

test Test0: main Main0 in (union { EspressoButtonMachine, Main0 }, CoffeeMaker);

test Test1: main Main1 in (union { SteamerButtonMachine, Main1 }, CoffeeMaker);

test Test2: main Main2 in (union { DoorMachine, Main2 }, CoffeeMaker);