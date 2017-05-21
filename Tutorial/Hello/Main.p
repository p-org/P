machine Main 
{
  var timer: TimerPtr;
  start state Init {  
    entry { 	
      timer = CreateTimer(this);
      goto PrintHello; 
    } 
  }

  state PrintHello {
    entry {
      print "Hello\n";
      StartTimer(timer, 100);
    }
    on TIMEOUT goto PrintHello;
  }
} 