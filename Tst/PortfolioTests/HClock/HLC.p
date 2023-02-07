event eNext;
event LRec : int;
event Send : int;

machine HLC {
  var pcL : map[int, int];
  var PC : map[int, int];
  var L : map[int, int];
  var C : map[int, int];
  var msg : map[int, (l : int, c : int)];
  var MaxT : int;
  var N : int;
  var Eps : int;
  var driver : machine;
  start state Init {
    entry (pld : (driver : machine, N : int, MaxT : int, Eps : int)) {
      var i : int;
      driver = pld.driver;
      N = pld.N;
      MaxT = pld.MaxT;
      Eps = pld.Eps;
      i = 1;
      while (i <= N) {
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
      var forall : bool;
      var choices : seq[int];
      j = 1;
      while (j <= N) {
        if (pcL[j] == 0) {
          if (PC[j] < MaxT) {
            pcL[j] = 1;
          }
        }
        if (pcL[j] == 1) {
          // forall cond
          forall = true;
          k = 1;
          while (k <= N) {
            if (!(PC[j] < PC[k] + Eps)) {
              forall = false;
            }
            k = k + 1;
          }
          if (forall) {
            choices += (sizeof(choices), j);
          }
        }
        j = j + 1;
      }
      if (sizeof(choices) > 0) {
        j = choose(choices);
        PC[j] = PC[j] + 1;
        if (choose()) {
          raise LRec, j;
        } else {
          raise Send, j;
        }
      }
    }
    on LRec do (j : int) {
      print("LRec");
      if (L[j] >= PC[j] && L[j] > msg[j].l) {
        C[j] = C[j] + 1;
      } else if (L[j] >= PC[j] && L[j]==msg[j].l) {
        if (msg[j].c > C[j]) {
          C[j] = msg[j].c + 1;
        } else {
          C[j] = C[j] + 1;
        }
      } else if (msg[j].l > L[j] && msg[j].l >= PC[j]) {
        L[j] = msg[j].l;
        C[j] = msg[j].c + 1;
      } else {
        L[j] = PC[j];
        C[j] = 0;
      }
      pcL[j] = 0;
      send driver, eNext;
    }
    on Send do (j : int) {
      var choices : seq[int];
      var i : int;
      print("Send");
      if (L[j] < PC[j]) {
        L[j] = PC[j];
        C[j] = 0;
      } else {
        C[j] = C[j] + 1;
      }
      i = 1;
      while (i <= N) {
        if (i != j) {
          if ((msg[i].l != L[j]) || (msg[i].c != C[j])) {
            choices += (sizeof(choices), i);
          }
        }
        i = i + 1;
      }
      assert(sizeof(choices) != 0);
      i = choose(sizeof(choices));
      msg[choices[i]] = ((l = L[j], c = C[j]));
      pcL[j] = 0;
      send driver, eNext;
    }
  }
}
machine Main {
  var hlc : machine;
  start state Init {
    entry {
      hlc = new HLC((driver=this, N=3, MaxT=5, Eps=2));
      send hlc, eNext;
    }
    on eNext do {
      send hlc, eNext;
    }
  }
}
