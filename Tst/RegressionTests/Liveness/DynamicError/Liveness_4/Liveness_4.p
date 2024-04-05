event x;
event y : machine;

machine Main {
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

  spec Mon1 observes x, y
  {
    var i : int;
    start state S1
    {
      entry {
        i = 0;
      }
      on y do (payload: machine) { i = i + 1; }
      on x goto hotState;
    }

    hot state hotState {

    }
  }

  machine M2
  {
    start state S1
    {
      on y do (payload: machine) { send payload, x; }
    }
  }

