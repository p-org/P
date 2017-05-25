machine TestMachine0
{
  var client: IClient;  
  start state Init {  
    entry { 	
      client = new Client(5); 
    } 
  }
} 

machine TestMachine1
{
  var client: IClient;  
  start state Init {  
    entry { 	
      client = new Client(-1); 
    } 
  }
} 
