event E1: (machine, machine);
event E2;
event E3: (ES1, ES2);

eventset ES1 = {E1, E2};
eventset ES2 = {E2, E3};
eventset ES3 = {E1, E2, E3};

machine Main {
  var x: machine;
  var y: ES2;

  start state S1 {
    entry {
      //valid
      send this, E1, (x, y);
      //valid
      x = y;
      //valid
      send this, E3, (x as ES1, y);
      //invalid
      send this, E3, (x, y);
    }
  }
}