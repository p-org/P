test saneUserUsingCoffeeMachine [main=TestWithSaneUser]:
  assert CoffeeMakerModesOfOperation in (union { TestWithSaneUser }, EspressoMachine, Users);

test crazyUserUsingCoffeeMachine [main=TestWithCrazyUser]:
  assert CoffeeMakerModesOfOperation in (union { TestWithCrazyUser }, EspressoMachine, Users);
