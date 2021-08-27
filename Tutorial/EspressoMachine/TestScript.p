module CoffeeMaker = { CoffeeMakerControllerMachine, CoffeeMakerMachine, Timer };

test Test0[main=Main0]: (union { EspressoButtonMachine, Main0 }, CoffeeMaker);

test Test1[main=Main1]: (union { SteamerButtonMachine, Main1 }, CoffeeMaker);

test Test2[main=Main2]: (union { DoorMachine, Main2 }, CoffeeMaker);