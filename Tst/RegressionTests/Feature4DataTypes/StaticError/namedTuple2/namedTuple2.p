type tRecord = (int, int);

machine Main {
  start state S1 {
    entry (x: any){
      	var b: (res: bool, record: tRecord, rId: int);
    	var c: (bool, tRecord, int);
    	b = (res = true, record = (0, 10), rId = 1);
        c = b;
        b.rId = 10;
        assert b.res == c.0 && b.rId == c.2 * 10;
    }
  }
}
