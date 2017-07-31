machine Hello
{
  var timer: TimerPtr;
  start state Init {  
    entry { 	
      timer = CreateTimer(this);
      goto GetInput; 
    } 
  }

  state GetInput {
	  entry {
      var b: bool;
      b = Continue();
      if (b) 
        goto PrintHello;
      else
        goto Stop;
	  }
  }

  state PrintHello {
    entry {
      StartTimer(timer, 100);
    }
    on TIMEOUT goto GetInput with {
      print "Hello\n";      
    }
  }

  state Stop { 
    entry {
      StopProgram();
    }
  }
}

