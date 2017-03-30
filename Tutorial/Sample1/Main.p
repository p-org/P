machine Main 
sends eEspressoButtonPressed, eSteamerButtonOn, eSteamerButtonOff;
creates ICoffeeMachine;
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