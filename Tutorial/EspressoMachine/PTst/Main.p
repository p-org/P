machine Main0 
receives ;
sends eInit;
{
  var coffeeMachine: CoffeeMakerMachine;
  var coffeeMachineController: CoffeeMakerControllerMachine;
  var espressoButton: machine;
  start state Init {  
    entry {
      coffeeMachineController = new CoffeeMakerControllerMachine(); 	
      coffeeMachine = new CoffeeMakerMachine(coffeeMachineController); 
      send coffeeMachineController, eInit, coffeeMachine;
      espressoButton = new EspressoButtonMachine((coffeeMachineController, 1));
    } 
  }
} 
