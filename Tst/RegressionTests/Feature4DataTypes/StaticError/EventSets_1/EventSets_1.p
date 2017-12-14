event E1: machine;
event E2;
event E3: (ES1, ES2);

eventset ES1 = {E1, E2};
eventset ES2 = {E2, E3};

machine Main {
  var x: ES1;
  var y: ES2;

  start state S1 {
    entry {
      //invalid
      x = y;
    }
  }
}