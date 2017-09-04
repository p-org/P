module CoffeeMaker = { ICoffeeMachineController -> CoffeeMachineController, ICoffeeMachine -> CoffeeMachine, ITimer -> Timer };

module Test0 = (compose { EspressoButton, Main0 }, CoffeeMaker);
test Test0: (rename Main0 to Main in Test0);

module Test1 = (compose { SteamerButton, Main1 }, CoffeeMaker);
test Test1: (rename Main1 to Main in Test1);

module Test2 = (compose { Door, Main2 }, CoffeeMaker);
test Test2: (rename Main2 to Main in Test2);