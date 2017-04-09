machine Main 
{
  var client: IClient;  
  start state Init {  
    entry { 	
      client = new Client(); 
    } 
  }
} 