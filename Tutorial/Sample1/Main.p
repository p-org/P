machine Main 
{
  var client: ICoffeeMachine;  
  start state Init {  
    entry { 	
      client = new ICoffeeMachine(); 
    } 
  }
} 