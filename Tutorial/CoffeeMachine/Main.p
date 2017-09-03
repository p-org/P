interface Main();

machine Main0 
receives ;
sends eInit;
{
  var coffeeMachine: ICoffeeMachine;
  var coffeeMachineController: ICoffeeMachineController;
  var espressoButton: machine;
  start state Init {  
    entry {
      coffeeMachineController = new ICoffeeMachineController(); 	
      coffeeMachine = new ICoffeeMachine(coffeeMachineController); 
      send coffeeMachineController, eInit, coffeeMachine;
      espressoButton = new EspressoButton((coffeeMachineController, 1));
    } 
  }
} 

machine Main1 
receives ;
sends eInit;
{
  var coffeeMachine: ICoffeeMachine;
  var coffeeMachineController: ICoffeeMachineController;
  var steamerButton: machine;
  start state Init {  
    entry {
      coffeeMachineController = new ICoffeeMachineController(); 	
      coffeeMachine = new ICoffeeMachine(coffeeMachineController); 
      send coffeeMachineController, eInit, coffeeMachine;
      steamerButton = new SteamerButton((coffeeMachineController, 1));
    } 
  }
} 

machine Main2 
receives ;
sends eInit;
{
  var coffeeMachine: ICoffeeMachine; 
  var coffeeMachineController: ICoffeeMachineController;
  var door: machine;

  start state Init {  
    entry { 	
      coffeeMachineController = new ICoffeeMachineController(); 
      coffeeMachine = new ICoffeeMachine(coffeeMachineController);
      send coffeeMachineController, eInit, coffeeMachine;
      door = new Door((coffeeMachineController, 1));
    } 
  }
} 
