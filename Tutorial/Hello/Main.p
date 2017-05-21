machine Main 
{
  var timer: Timer;
  start state Init {  
    entry { 	
      timer = CreateTimer(this);
      goto PrintHello; 
    } 
  }

  state PrintHello {
    entry {
      print "Hello";
      StartTimer(timer, 100);
    }
    on TIMEOUT goto PrintHello;
  }
} 