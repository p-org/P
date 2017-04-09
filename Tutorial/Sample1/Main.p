machine Main0 
sends eEspressoButtonPressed, eSteamerButtonOn, eSteamerButtonOff;
{
  var client: ICoffeeMachine;  
  start state Init {  
    entry { 	
      client = new ICoffeeMachine(); 
      send client, eEspressoButtonPressed;
      send client, eSteamerButtonOn;
      send client, eSteamerButtonOff;
    } 
  }
} 

machine Main1 
sends eEspressoButtonPressed, eDoorOpened, eDoorClosed;
{
  var client: ICoffeeMachine;  
  start state Init {  
    entry { 	
      client = new ICoffeeMachine(); 
      send client, eEspressoButtonPressed;
      send client, eDoorOpened;
      send client, eDoorClosed;
    } 
  }
} 
