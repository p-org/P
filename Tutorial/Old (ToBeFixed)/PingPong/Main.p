machine Test_1_Machine
receives;
sends;
{
  var client: ClientMachine;  
  start state Init {  
    entry { 	
      client = new ClientMachine(5); 
    } 
  }
} 

machine Test_2_Machine
receives;
sends;
{
  var client: ClientMachine;  
  start state Init {  
    entry { 	
      client = new ClientMachine(-1); 
    } 
  }
} 
