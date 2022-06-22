event ev : any;

machine Main {
  start state Init {
    entry {
      send this, ev, (s = null,);
    }

    on ev do (x: any) { }
  }
}
