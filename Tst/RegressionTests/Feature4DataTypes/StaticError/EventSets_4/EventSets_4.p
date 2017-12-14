event E1: (machine, machine);
event E2;
event E3: (ES1, ES2);

eventset ES1 = {E1, E2};
eventset ES2 = {E2, E3};
eventset ES3 = {E1, E2, E3};

machine Main {
  var x: ES3;
  var y: machine;

  start state S1 {
    entry {
      var z : ES1;
      //valid
      send this, E1, (x, y);
      //valid
      x = y;
      //valid
      z = x to ES1;
      //valid
      send this, E3, (x to ES1, y);
      //invalid
      send this, E3, (x to ES2, y);
    }
  }
}