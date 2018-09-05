machine Main {
  var x: (a: int, b: float);
  start state S1 {
    entry (x: any){
      goto S1, (a = 10, a = 10);
    }
  }
}
