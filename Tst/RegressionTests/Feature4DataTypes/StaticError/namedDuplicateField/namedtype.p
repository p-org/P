machine Main {
  var x: (a: int, a: float);
  start state S1 {
    entry (x: any){
      goto S1, (a = 10, b = 10);
    }
  }
}
