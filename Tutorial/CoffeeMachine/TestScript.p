module CoffeeMaker = { ICoffeeMachineController -> CoffeeMachineController, ICoffeeMachine -> CoffeeMachine, ITimer -> Timer };

test Test0: main Main0 in (union { EspressoButton, Main0 }, CoffeeMaker);

test Test1: main Main1 in (union { SteamerButton, Main1 }, CoffeeMaker);

test Test2: main Main2 in (union { Door, Main2 }, CoffeeMaker);