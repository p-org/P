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

machine Main1 
receives ;
sends eInit;
{
  var coffeeMachine: CoffeeMakerMachine;
  var coffeeMachineController: CoffeeMakerControllerMachine;
  var steamerButton: machine;
  start state Init {  
    entry {
      coffeeMachineController = new CoffeeMakerControllerMachine(); 	
      coffeeMachine = new CoffeeMakerMachine(coffeeMachineController); 
      send coffeeMachineController, eInit, coffeeMachine;
      steamerButton = new SteamerButtonMachine((coffeeMachineController, 1));
    } 
  }
} 

machine Main2 
receives ;
sends eInit;
{
  var coffeeMachine: CoffeeMakerMachine; 
  var coffeeMachineController: CoffeeMakerControllerMachine;
  var door: machine;

  start state Init {  
    entry { 	
      coffeeMachineController = new CoffeeMakerControllerMachine(); 
      coffeeMachine = new CoffeeMakerMachine(coffeeMachineController);
      send coffeeMachineController, eInit, coffeeMachine;
      door = new DoorMachine((coffeeMachineController, 1));
    } 
  }
} 
