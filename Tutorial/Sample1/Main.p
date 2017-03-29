machine Main 
{
  var client: machine;  
  start state Init {  
    entry { 	
      client = new CoffeeMachine(); 
    } 
  }
} 