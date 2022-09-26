event eNext;

event Phase1a : int;
event Phase2a : (int, int);
event Phase1b : (int, int);
event Phase2b : (int, int, int);

machine Paxos {
  // ballots
  //var M1a : set[int];
  // acceptor, ballot, message ballot, value (not used because of the Q1bs)
  // var M1b : set[(int, int, int, int)];
  // ballot -> val
  var M2a : map[int, set[int]];
  // acceptor -> ballot -> val
  var M2b : map[int, map[int, set[int]]];
  // quorum -> ballot -> acc
  var Q1bAcc : map[int, map[int, set[int]]];
  // quorum -> ballot -> val
  var Q1bVal : map[int, map[int, set[int]]];
  // quorum -> ballot -> val
  var Q1bBal : map[int, map[int, set[int]]];
  // quorum -> bool
  var forallCond : map[int, bool];
  // quorum -> ballot
  var maxQ1Bal : map[int, int];
  var maxQ1Val : map[int, int];
  var maxBal : seq[int];
  //var maxVBal : seq[int];
  var maxVal : seq[int];
  var driver : machine;
  var quorum : seq[seq[int]];
  var numQuora : int;
  var NumAcceptors : int;
  var NumVals : int;
  var NumBallots : int;
  var notM1a : set[int];
  start state Init {
    entry (pld : (driver : machine, NumAcceptors : int, NumVals : int, NumBallots : int)) {
      var i : int;
      var quorumSeq : seq[int];
      var intSet : set[int];
      var ballotIntSet : map[int, set[int]];
      driver = pld.driver;
      NumAcceptors = pld.NumAcceptors;
      NumVals = pld.NumVals;
      NumBallots = pld.NumBallots;
      numQuora = 1;
      quorumSeq += (0, 0);
      quorumSeq += (1, 1);
      quorum += (0, quorumSeq);
      i = 0;
      while (i < NumBallots) {
        ballotIntSet += (i, intSet);
        notM1a += (i);
        i = i + 1;
      }
      M2a = ballotIntSet;
      i = 0;
      while (i < NumAcceptors) {
        maxBal += (i, -1);
        //maxVBal += (i, -1);
        maxVal += (i, -1);
        M2b += (i, ballotIntSet);
        i = i + 1;
      }
      Q1bAcc += (0, ballotIntSet);
      Q1bVal += (0, ballotIntSet);
      Q1bBal += (0, ballotIntSet);
      forallCond += (0, false);
      maxQ1Bal += (0, -1);
      maxQ1Val += (0, -1);
    }
    on eNext do {
      var i : int;
      var j : int;
      var k : int;
      var choices : seq[(int, int, int)];
      var choice : (int, int, int);
      var pcChoice : int;
      var arg : int;
      var ballot : int;
      // ballot check
      while (i < sizeof(notM1a)) {
        // Phase1a(b)
        ballot = notM1a[i];
        choices += (sizeof(choices), (0, ballot, 0));
        j = 0;
        while (j < numQuora) {
          if (forallCond[j]) {
            if (!(i in M2a)) {
              // \A a \in Q : \E m \in Q1b : m.acc = a
              if (sizeof(Q1bVal[j][ballot]) == 0) {
                // Q1bv = {}
                k = 0;
                while (k < NumVals) {
                  choices += (sizeof(choices), (1, ballot, k));
                  k = k + 1;
                }
              } else if (ballot >= maxQ1Bal[j]) {
                // \A mm \in Q1bv : m.mbal \geq mm.mbal 
                k = 0;
                while (k < NumVals) {
                  if (k in Q1bVal[j][ballot]) {
                     // m.mval = v
                    choices += (sizeof(choices), (1, ballot, k));
                  }
                  k = k + 1;
                }
              }
            }
          }
          j = j + 1;
        }
        i = i + 1;
      }
      i = 0;
      while (i < NumAcceptors) {
        j = maxBal[i];
        while (j < NumBallots) {
          if ((j > maxBal[i]) && !(j in notM1a)) {
            // Phase1b
            choices += (sizeof(choices), (2, i, j));
          } 
          // Phase2b
          if (j in M2a) {
            if (!(0 in M2b[i][j])) {
              choices += (sizeof(choices), (3, i, j));
            }
            if (!(1 in M2b[i][j]))) {
              choices += (sizeof(choices), (4, i, j));
            }
          }
          j = j + 1;
        }
        i = i + 1;
      }
      if (sizeof(choices) > 0) {
        choice = choices[choose(sizeof(choices))];
        pcChoice = choice.0;
        arg = choice.1;
        if (pcChoice == 0) {
          raise Phase1a, arg;
        }
        if (pcChoice == 1) {
          raise Phase2a, (arg, choice.2);
        }
        if (pcChoice == 2) {
          raise Phase1b, (arg, choice.2);
        }
        if (pcChoice == 3) {
          raise Phase2b, (arg, choice.2, 0);
        }
        if (pcChoice == 4) {
          raise Phase2b, (arg, choice.2, 1);
        }
      }
    }
    on Phase1a do (b : int) {
      print("Phase1a");
      //M1a += (b);
      notM1a -= (b);
      send driver, eNext;
    }

    on Phase2a do (pld : (int, int)) {
      var b : int;
      var v : int;
      print("Phase2a");
      b = pld.0;
      v = pld.1;
      M2a[b] += (v);
      send driver, eNext;      
    }

    on Phase1b do (pld : (int, int)) {
      var a : int;
      var b : int;
      var i : int;
      var q : int;
      print("Phase1b");
      a = pld.0;
      b = pld.1;
      //M1b += (a, b, maxVBal[a], maxVal[a]);
      while (i < numQuora) {
        if (a in quorum[i]) {
          q = i;
        }
        i = i + 1;
      }
      Q1bAcc[q][b] += (a);
      //Q1bVal[q][b] += (maxVBal[a]);
      Q1bVal[q][b] += (maxVal[a]);
      if (b > maxQ1Bal[q]) {
        maxQ1Bal[q] = b;
      }
      if (maxVal[a] > maxQ1Val[q]) {
        maxQ1Val[q] = maxVal[a];
      }
      if (!(forallCond[q])) {
        forallCond[q] = true;
        i = 0;
        while (i < sizeof(quorum[q])) {
          if (!(quorum[q][i] in Q1bAcc[q][b])) {
            forallCond[q] = false;
          }
          i = i + 1;
        }
      }
      send driver, eNext;
    }

    on Phase2b do (pld : (int, int)) {
      var a : int;
      var b : int;
      var i : int;
      var choices : seq[int];
      var v : int;
      var valSet : set[int];
      var valSeq : seq[int];
      var valMap : map[int, set[int]];
      print("Phase2b");
      a = pld.0;
      b = pld.1;
      //while (i < NumVals) { 
      //  if (!(i in M2b[a][b])) {
      //    choices += (sizeof(choices), i);
      //  }
      //  i = i + 1;
      //} 
      v = pld.2; //choose(choices);
      maxBal[a] = b;
      //maxVBal[a] = b;
      maxVal[a] = v;
      //valSet = M2b[a][b];
      M2b[a][b] += (v);
      send driver, eNext;
    }
  }
}

machine Main {
  var paxos : machine;
  start state Init {
    entry {
      paxos = new Paxos((driver=this, NumAcceptors = 3, NumVals = 2, NumBallots = 3));
      send paxos, eNext;
    }
    on eNext do {
      send paxos, eNext;
    }
  }
} 
