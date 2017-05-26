machine TestMachine0
receives ;
sends ;
{
  var client: IClient;  
  start state Init {  
    entry { 	
      client = new IClient(5); 
    } 
  }
} 

machine TestMachine1
receives ;
sends ;
{
  var client: IClient;  
  start state Init {  
    entry { 	
      client = new IClient(-1); 
    } 
  }
} 
