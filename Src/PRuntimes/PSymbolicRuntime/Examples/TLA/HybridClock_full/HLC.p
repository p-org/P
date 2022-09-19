event eNext;
event S : int;
event After : int;
event LRec : int;
event Send : int;

machine HLC {
  var pcL : map[int, int];
  var PC : map[int, int];
  var L : map[int, int];
  var C : map[int, int];
  var msg : map[int, (l : int, c : int)];
  var minPC : int;
  var MaxT : int;
  var N : int;
  var Eps : int;
  var driver : machine;
  var Procs : set[int];
  start state Init {
    entry (pld : (driver : machine, N : int, MaxT : int, Eps : int)) {
      var i : int;
      driver = pld.driver;
      N = pld.N;
      MaxT = pld.MaxT;
      Eps = pld.Eps;
      i = 1;
      while (i <= N) {
        Procs += (i);
        pcL[i] = 0;
        PC[i] = 0;
        L[i] = 0;
        C[i] = 0;
        msg[i] = ((l = 0, c = 0));
        i = i + 1;
      } 
    }
    on eNext do {
      var j : int;
      var k : int;
      var choices : seq[int];
      var choice : bool;
      j = 1;
      while (j <= N) {
        if (pcL[j] == 0) {
          if (PC[j] < MaxT) {
            choices += (sizeof(choices), j);
          }
        }
        if (pcL[j] == 1) {
          // forall cond
          if (PC[j] < minPC + Eps) {
            choices += (sizeof(choices), j);
          }
        }
        j = j + 1;
      }
      if (sizeof(choices) > 0) {
        j = choose(choices);
        if (pcL[j] == 0) {
          raise S, j;
        }
        else {
          raise After, j;
        }
      }
    }
    on S do (j : int) {
      pcL[j] = 1;
      send driver, eNext;
    }

    on After do (j : int) {
      var choice : bool;
      var otherProcs : set[int];
      var k : int;
      PC[j] = PC[j] + 1;
      pcL[j] = 0;
      send driver, eNext;
      choice = choose();
      if (L[j] >= PC[j]) {
        if (choice || L[j] > msg[j].l) {
          C[j] = C[j] + 1;
        } else if (L[j] == msg[j].l) {
          if (msg[j].c > C[j]) {
            C[j] = msg[j].c + 1;
          } else {
            C[j] = C[j] + 1;
          }
        } 
      } else {
        if (choice || msg[j].l <= L[j] || msg[j].l < PC[j]) {
          L[j] = PC[j];
          C[j] = 0;
        } else {
          L[j] = msg[j].l;
          C[j] = msg[j].c + 1;
        }
      }
      if (choice) {
         otherProcs = Procs;
         otherProcs -= j;
         msg[choose(otherProcs)] = ((l = L[j], c = C[j]));
      }
      k = 2;
      minPC = PC[1];
      while (k <= N) {
        if (PC[k] < minPC) {
          minPC = PC[k];
        }
        k = k + 1;
      }
    }
  }
}
machine Main {
  var hlc : machine;
  start state Init {
    entry {
      hlc = new HLC((driver=this, N=3, MaxT=3, Eps=2));
      send hlc, eNext;
    }
    on eNext do {
      send hlc, eNext;
    }
  }
}
