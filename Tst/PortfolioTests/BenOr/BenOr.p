event eNext;
event EvalP1 : (process : int, val : int);
event EvalP2Case1 : (process : int, val : int);
event EvalP2Case2 : (process : int, val : int);
event EvalP2Case3 : int;

machine BenOr {
  var pc : map[int, int];
  var r : map[int, int];
  var p1v : map[int, int];
  var p2v : map[int, int];
  var decided : map[int, int];
  var processes : seq[int];
  var N : int;
  var MAXROUND : int;
  var F : int;
  var driver : machine;
  var SentP1Msgs : map[int, seq[(int, int)]];
  var SentP1MsgsV : map[int, map[int, seq[int]]];
  var ValSetP1Msgs : map[int, seq[int]];
  var SentP2Msgs : map[int, seq[(int, int)]];
  var SentP2MsgsV : map[int, map[int, seq[int]]];
  var ValSetP2Msgs : map[int, seq[int]];

  start state Init {
    entry (pld : (driver : machine, N : int, INPUT : seq[int], MAXROUND : int, F : int)) {
      var i : int;
      var pairSeq : seq[(int, int)];
      var intSeq : seq[int];
      var valMap : map[int, seq[int]];
      driver = pld.driver;
      N = pld.N;
      MAXROUND = pld.MAXROUND;
      F = pld.F;
      i = 0;
      while (i < N) {
        processes += (i, i + 1);
        pc[i + 1] = 0;
        r[i + 1] = 1;
        p1v[i + 1] = pld.INPUT[i];
        p2v[i + 1] = -1;
        decided[i + 1] = -1;
        i = i + 1;
      }
      i = -1;
      while (i < 2) {
        valMap[i] = intSeq;
        i = i + 1;
      }
      i = -1;
      while (i <= MAXROUND) {
        SentP1Msgs[i] = pairSeq;
        SentP1MsgsV[i] = valMap;
        ValSetP1Msgs[i] = intSeq;
        SentP2Msgs[i] = pairSeq;
        SentP2MsgsV[i] = valMap;
        ValSetP2Msgs[i] = intSeq;
        i = i + 1;
      }
    }

    on eNext do {
      var i : int;
      var j : int;
      var choices : seq[(int, int, int)];
      var choice : (int, int, int);
      var added : bool;
      var change : bool;
      i = 0;
      while (i < N) {
        if (pc[processes[i]] == 0) {
          if (r[processes[i]] <= MAXROUND) {
            if (!((processes[i], p1v[processes[i]]) in SentP1Msgs[r[processes[i]]])) {
              SentP1Msgs[r[processes[i]]] += (sizeof(SentP1Msgs[r[processes[i]]]), (processes[i], p1v[processes[i]]));
              SentP1MsgsV[r[processes[i]]][p1v[processes[i]]] += (sizeof(SentP1MsgsV[r[processes[i]]][p1v[processes[i]]]), processes[i]);
              if (!(p1v[processes[i]] in ValSetP1Msgs[r[processes[i]]])) {
                ValSetP1Msgs[r[processes[i]]] += (sizeof(ValSetP1Msgs[r[processes[i]]]), p1v[processes[i]]);
              }
              change = true;
            }
            p2v[processes[i]] = -1; 
            pc[processes[i]] = 1;
          }
        }
        else if (pc[processes[i]] == 1) {
          if (sizeof(SentP1Msgs[r[processes[i]]]) >= N - F) {
            // can do EvalP1(r)
            if (2 * sizeof(SentP1MsgsV[r[processes[i]]][0]) > N ||
                2 * sizeof(SentP1MsgsV[r[processes[i]]][1]) > N) {
              // make sure there is a choice that will change the value of p2v
              j = 0;
              while (j < sizeof(ValSetP1Msgs[r[processes[i]]])) {
                if (2 * sizeof(SentP1MsgsV[r[processes[i]]][ValSetP1Msgs[r[processes[i]]][j]]) > N) {
                  if (ValSetP1Msgs[r[processes[i]]][j] != p2v[processes[i]]) {
                    choices += (sizeof(choices), (0, processes[i], ValSetP1Msgs[r[processes[i]]][j]));
                  }
                }
                j = j + 1;
              }
            } else {
              // does not execute the branch in Eval1, so advance to CP2
              if (!((processes[i], p2v[processes[i]]) in SentP2Msgs[r[processes[i]]])) {
                change = true;
                SentP2Msgs[r[processes[i]]] += (sizeof(SentP2Msgs[r[processes[i]]]), (processes[i], p2v[processes[i]]));
                SentP2MsgsV[r[processes[i]]][p2v[processes[i]]] += (sizeof(SentP2MsgsV[r[processes[i]]][p2v[processes[i]]]), processes[i]);
                if (!(p2v[processes[i]] in ValSetP2Msgs[r[processes[i]]])) {
                  ValSetP2Msgs[r[processes[i]]] += (sizeof(ValSetP2Msgs[r[processes[i]]]), p2v[processes[i]]);
                }
                pc[processes[i]] = 2;
              }
            }
          }
        }
        else if (pc[processes[i]] == 2) {
          if (sizeof(SentP2Msgs[r[processes[i]]]) >= N - F) {
            // can do EvalP2(r)
            if (sizeof(SentP2MsgsV[r[processes[i]]][0]) > F ||
                sizeof(SentP2MsgsV[r[processes[i]]][1]) > F) {
              j = 0;
              while (j < sizeof(ValSetP2Msgs[r[processes[i]]])) {
                if (ValSetP2Msgs[r[processes[i]]][j] != -1) {
                  if ((p1v[processes[i]] != ValSetP2Msgs[r[processes[i]]][j]) ||
                      (decided[processes[i]] != ValSetP2Msgs[r[processes[i]]][j])) {
                    choices += (sizeof(choices), (1, processes[i], ValSetP2Msgs[r[processes[i]]][j]));
                  }
                }
                j = j + 1;
              }
            } else {
              j = 0;
              added = false;
              while (j < sizeof(ValSetP2Msgs[r[processes[i]]])) {
                if (ValSetP2Msgs[r[processes[i]]][j] != -1) {
                  if (p1v[processes[i]] != ValSetP2Msgs[r[processes[i]]][j]) {
                    choices += (sizeof(choices), (2, processes[i], ValSetP2Msgs[r[processes[i]]][j]));
                    added = true;
                  }
                }
                j = j + 1;
              }
              if (!added) {
                choices += (sizeof(choices), (3, processes[i], 0));
              }
            }
          }
        }
        i = i + 1;
      }
      if (sizeof(choices) > 0) {
        i = choose(sizeof(choices));
        choice = choices[i];
        if (choice.0 == 0) {
          raise EvalP1, (process=choice.1, val=choice.2);
        } else if (choice.0 == 1) {
          raise EvalP2Case1, (process=choice.1, val=choice.2);
        } else if (choice.0 == 2) {
          raise EvalP2Case2, (process=choice.1, val=choice.2);
        } else if (choice.0 == 3) {
          raise EvalP2Case3, (choice.1);
        }
      } else if (change) {
         send driver, eNext;
      }
    }
    
    on EvalP1 do (pld: (process : int, val : int)) {
      print("EvalP1");
      p2v[pld.process] = pld.val;
      if (!((pld.process, p2v[pld.process]) in SentP2Msgs[r[pld.process]])) {
        SentP2Msgs[r[pld.process]] += (sizeof(SentP2Msgs[r[pld.process]]), (pld.process, p2v[pld.process]));
        SentP2MsgsV[r[pld.process]][p2v[pld.process]] += (sizeof(SentP2MsgsV[r[pld.process]][p2v[pld.process]]), pld.process);
        if (!(p2v[pld.process] in ValSetP2Msgs[r[pld.process]])) {
          ValSetP2Msgs[r[pld.process]] += (sizeof(ValSetP2Msgs[r[pld.process]]), p2v[pld.process]);
        }
      }
      pc[pld.process] = 2;
      send driver, eNext;
    }

    on EvalP2Case1 do (pld: (process : int, val : int)) {
      print("EvalP2 - case 1");
      p1v[pld.process] = pld.val;
      decided[pld.process] = pld.val;
      r[pld.process] = r[pld.process] + 1;
      pc[pld.process] = 0;
      send driver, eNext;
    }

    on EvalP2Case2 do (pld: (process : int, val : int)) {
      print("EvalP2 - case 2");
      p1v[pld.process] = pld.val;
      r[pld.process] = r[pld.process] + 1;
      pc[pld.process] = 0;
      send driver, eNext;
    }

    on EvalP2Case3 do (process : int) {
      var i : int;
      print("EvalP2 - case 3");
      if (p1v[process] == 0) {
        p1v[process] = 1;
      } else if (p1v[process] == 1) {
        p1v[process] = 0;
      } else {
        p1v[process] = choose(1);
      } 
      r[process] = r[process] + 1;
      pc[process] = 0;
      send driver, eNext;
    }
  }
}


machine Main {
  var benor : machine;
  start state Init {
    entry {
      var N : int;
      var INPUT : seq[int];
      var MAXROUND : int;
      var F : int;
      F = 2;
      N = 4;
      INPUT += (0, 0);
      INPUT += (1, 1);
      INPUT += (2, 1);
      INPUT += (3, 1);
      MAXROUND = 3;
      benor = new BenOr((driver=this, N=N, INPUT=INPUT, MAXROUND=MAXROUND, F=F));
      send benor, eNext;
    }
    on eNext do {
      send benor, eNext;
    }
  }
}
