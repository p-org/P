machine Main0 
receives ;
sends eInit, eEspressoButtonPressed, eSteamerButtonOn, eSteamerButtonOff;
{
  var coffeeMachine: ICoffeeMachine;
  var coffeeMachineController: ICoffeeMachineController;

  start state Init {  
    entry {
      coffeeMachineController = new ICoffeeMachineController(); 	
      coffeeMachine = new ICoffeeMachine(coffeeMachineController); 
      send coffeeMachineController, eInit, coffeeMachine;
      send coffeeMachineController, eEspressoButtonPressed;
      send coffeeMachineController, eSteamerButtonOn;
      send coffeeMachineController, eSteamerButtonOff;
    } 
  }
} 

machine Main1 
receives ;
sends eInit, eEspressoButtonPressed;
{
  var coffeeMachine: ICoffeeMachine; 
  var coffeeMachineController: ICoffeeMachineController;
  var user: machine;

  start state Init {  
    entry { 	
      coffeeMachineController = new ICoffeeMachineController(); 
      coffeeMachine = new ICoffeeMachine(coffeeMachineController);
      send coffeeMachineController, eInit, coffeeMachine;
      user = new User((coffeeMachineController, 1));
      send coffeeMachineController, eEspressoButtonPressed;
    } 
  }
} 
