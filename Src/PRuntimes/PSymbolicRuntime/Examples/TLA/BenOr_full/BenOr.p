event eNext;
event S : int;
event EvalP1 : int;
event EvalP2 : int;

machine BenOr {
  var pc : seq[int];
  var r : seq[int];
  var p1v : seq[int];
  var p2v : seq[int];
  var decided : seq[int];
  var N : int;
  var MAXROUND : int;
  var F : int;
  var driver : machine;
  var SentP1Msgs : seq[set[(int, int)]];
  var SentP1MsgsV : seq[map[int, set[int]]];
  //var ValSetP1Msgs : seq[set[int]];
  var SentP2Msgs : seq[set[(int, int)]];
  var SentP2MsgsV : seq[map[int, set[int]]];
//  var ValSetP2Msgs : seq[set[int]];

  start state Init {
    entry (pld : (driver : machine, N : int, INPUT : seq[int], MAXROUND : int, F : int)) {
      var i : int;
      var pairSet : set[(int, int)];
      var intSet : set[int];
      var valMap : map[int, set[int]];
      driver = pld.driver;
      N = pld.N;
      MAXROUND = pld.MAXROUND;
      F = pld.F;
      i = 0;
      while (i < N) {
        pc += (i, 0);
        r += (i, 1);
        p1v += (i, pld.INPUT[i]);
        p2v += (i, -1);
        decided += (i, -1);
        i = i + 1;
      }
      i = -1;
      while (i < 2) {
        valMap += (i, intSet);
        i = i + 1;
      }
      i = 0;
      while (i <= MAXROUND) {
        SentP1Msgs += (i, pairSet);
        SentP1MsgsV += (i, valMap);
        //ValSetP1Msgs += (i, intSet);
        SentP2Msgs += (i, pairSet);
        SentP2MsgsV += (i, valMap);
        //ValSetP2Msgs += (i, intSet);
        i = i + 1;
      }
    }

    on eNext do {
      var i : int;
      var j : int;
      var choices : seq[(int, int)];
      var choice : (int, int);
      var added : bool;
      i = 0;
      while (i < N) {
        if (pc[i] == 0) {
          if (r[i] <= MAXROUND) {
            choices += (sizeof(choices), (0, i));
          }
        }
        else if (pc[i] == 1) {
          if (sizeof(SentP1Msgs[r[i]]) >= N - F) {
            // can do EvalP1(r)
            if (2 * sizeof(SentP1MsgsV[r[i]][0]) > N ||
                2 * sizeof(SentP1MsgsV[r[i]][1]) > N) {
              choices += (sizeof(choices), (1, i));
            }
          }
        }
        else if (pc[i] == 2) {
          if (sizeof(SentP2Msgs[r[i]]) >= N - F) {
            // can do EvalP2(r)
            if (sizeof(SentP2MsgsV[r[i]][0]) > F ||
                sizeof(SentP2MsgsV[r[i]][1]) > F) {
              j = 0;
              choices += (sizeof(choices), (2, i));
            }
          }
        }
        i = i + 1;
      }
      if (sizeof(choices) > 0) {
        i = choose(sizeof(choices));
        choice = choices[i];
        if (choice.0 == 0) {
          raise S, choice.1;
        } else if (choice.0 == 1) {
          raise EvalP1, choice.1;
        } else {
          raise EvalP2, choice.1;
        }
      }
    }

    on S do (process : int) {
      var round : int;
      round = r[process];
      print("S");
      SentP1Msgs[round] += ((process, p1v[process]));
      SentP1MsgsV[round][p1v[process]] += (process);
      //ValSetP1Msgs[round] += (p1v[process]);
      p2v[process] = -1; 
      pc[process] = 1;
      send driver, eNext;
    }
    
    on EvalP1 do (process : int) {
      var round : int;
      var choices : seq[int];
      var i : int;
      round = r[process];
      print("EvalP1");
      if (2 * sizeof(SentP1MsgsV[round][0]) > N) {
        choices += (sizeof(choices), 0);
      }
      if (2 * sizeof(SentP1MsgsV[round][1]) > N) {
        choices += (sizeof(choices), 1);
      }
      if (sizeof(choices) > 0) {
        p2v[process] = choose(choices);
      }
      SentP2Msgs[round] += ((process, p2v[process]));
      SentP2MsgsV[round][p2v[process]] += (process);
      //ValSetP2Msgs[round] += (p2v[process]);
      pc[process] = 2;
      send driver, eNext;
    }

    on EvalP2 do (process : int) {
      var round : int;
      var choices : seq[int];
      round = r[process];
      print("EvalP2");
      if (sizeof(SentP2MsgsV[round][0]) > 0) {
        choices += (sizeof(choices), 0);
      }
      if (sizeof(SentP2MsgsV[round][1]) > 0) {
        choices += (sizeof(choices), 1);
      }
      if (sizeof(choices) > 0) {
        p1v[process] = choose(choices);
      } else {
        p1v[process] = choose(2);
      }
      if ((sizeof(SentP2MsgsV[round][0])) > F ||
          (sizeof(SentP2MsgsV[round][1])) > F) {
        decided[process] = p1v[process];
      }
      r[process] = round + 1;
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
      F = 1;
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
