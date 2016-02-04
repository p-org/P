event x;
event y : machine;

  main machine M1
  {
    var id: machine;
    start state S 
    {
      entry {
        id = new M2();
        send id, y, this;
      }
      ignore x;
    }
  }
  
  spec Mon1 monitors x, y
  {
    var i : int;
    start state S1
    {
      entry {
        i = 0;
      }
      on y do { i = i + 1; };
      on x goto hotState;
    }

    hot state hotState {
      
    }
  }

  machine M2
  {
    start state S1
    {
      on y do (payload: machine) { send payload, x; };
    }
  }

