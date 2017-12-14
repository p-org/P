event E1: (machine, machine);
event E2;
event E3: (ES1, ES2);

machine ES1 receives E1, E2; {
  start state S {}
}
machine ES2 receives E2, E3; {
  start state S {}
}
machine ES3 receives E1, E2, E3; {
  start state S {}
}

machine Main {
  var x: ES2;
  var y: machine;

  start state S1 {
    entry {
      var z : ES1;
      //valid
      send this, E1, (x, y);
      //valid
      x = y as ES2;
      //invalid
      z = x to ES1;
    }
  }
}