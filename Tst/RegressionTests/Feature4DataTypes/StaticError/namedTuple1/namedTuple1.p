type tRecord;

machine Main {
  start state S1 {
    entry (x: any){
      	var b: (res: bool, record: tRecord, rId: int);
    	var c: (bool, tRecord, int);
        b = c;
    }
  }
}
