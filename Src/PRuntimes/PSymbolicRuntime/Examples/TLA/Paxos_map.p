event eSendP1 : int;
event eCollectP1 : int;
event eSendP2 : int;
event eCollectP2 : int;
event eReplyP1 : (acceptor : int, b : int);
event eReplyP2 : (acceptor : int, b : int);
event eRecvP3 : (acceptor : int, b : int);
event eNext;

machine Global {
  var acceptors : seq[int];
  var leaders : seq[int];
  var vals : seq[int];
  var slots : seq[int];
  var ballots : seq[int];
  // acceptor
  var maxBal : map[int, int];
  var hVal : map[int, map[int, map[int, map[int, bool]]]];
  var decided : map[int, map[int, map[int, bool]]];
  // leader
  var b : map[int, int];
  var s : map[int, int];
  var elected : map[int, bool];
  var lv : map[int, int];
  var pVal : map[int, map[int, map[int, map[int, bool]]]];
  var pc : map[int, int];
  // quora
  var q1 : seq[map[int, bool]];
  var q2 : seq[map[int, bool]];
  // globals
  // ballot set
  var SentP1A : map[int, bool];
  // ballot, slot, value set
  var SentP2A : map[int, map[int, map[int, map[int, bool]]]];
  // ballot, slot, value set
  var SentP3A : map[int, map[int, map[int, bool]]];
  // ballot, ballot, slot, value set
  var SentP1B : map[int, map[int, map[int, map[int, bool]]]];
  // ballot, value set
  var SentP2B : map[int, map[int, bool]];
  var p1BSenderBallot : map[int, map[int, bool]];
  var p2BSenderBallot : map[int, map[int, bool]];
  var SatQ1 : map[int, bool];
  var SatQ2 : map[int, bool];
  var highestP1ABallot : int;
  var driver : machine;
  var M : int;

  start state Init {
    entry (pld : (driver : machine, acceptSeq : seq[int], leadSeq : seq[int],
           slotSeq : seq[int], ballotSeq : seq[int], M : int)) {
      var i : int;
      var j : int;
      var emptyVSet : map[int, bool];
      var emptySlotSet : map[int, map[int, bool]];
      var emptyValSet : map[int, map[int, map[int, bool]]];
      var emptyBalValSet : map[int, map[int, map[int, map[int, bool]]]];
      var quorumMap : map[int, bool]; 
      driver = pld.driver;
      highestP1ABallot = -1;
      acceptors = pld.acceptSeq;
      leaders = pld.leadSeq;
      slots = pld.slotSeq; 
      ballots = pld.ballotSeq;
      vals = leaders;
      vals += (sizeof(vals), -1);
      emptyVSet[-1] = false;
      i = 0;
      while (i < sizeof(vals)) {
        emptyVSet[vals[i]] = false;
        i = i + 1;
      }
      i = 0;
      emptySlotSet[-1] = emptyVSet;
      while (i < sizeof(slots)) {
        emptySlotSet[slots[i]] = emptyVSet;
        i = i + 1;
      }
      i = 0;
      emptyValSet[-1] = emptySlotSet;
      while (i < sizeof(ballots)) {
        emptyValSet[ballots[i]] = emptySlotSet;
        i = i + 1;
      }
      // acceptor initialization
      i = 0;
      while (i < sizeof(acceptors)) {
        maxBal += (acceptors[i], -1);
        hVal += (acceptors[i], emptyValSet);
        hVal[acceptors[i]][-1][-1][-1] = true;
        decided += (acceptors[i], emptySlotSet);
        i = i + 1;
      }
      // leader initialization
      i = 0;
      while (i < sizeof(leaders)) {
        b += (leaders[i], leaders[i] - sizeof(acceptors));
        s += (leaders[i], 1);
        elected += (leaders[i], false);
        lv += (leaders[i], -1);
        pVal += (leaders[i], emptyValSet);
        pVal[leaders[i]][-1][-1][-1] = true;
        pc += (leaders[i], 0);
        i = i + 1;
      }
      // sent acc msgs
      i = 0;
      while (i < sizeof(ballots)) {
        SentP1A += (ballots[i], false);
        SentP2A += (ballots[i], emptyValSet);
        SentP3A += (ballots[i], emptySlotSet);
        SentP1B += (ballots[i], emptyValSet);
        SentP2B += (ballots[i], emptyVSet);
        SatQ1[ballots[i]] = false;
        SatQ2[ballots[i]] = false;
        i = i + 1;
      }
      i = 0;
      while (i < sizeof(acceptors)) {
        p1BSenderBallot[acceptors[i]] = SatQ1;
        p2BSenderBallot[acceptors[i]] = SatQ1;
        i = i + 1;
      }
      quorumMap += (1, false);
      quorumMap += (2, false);
      quorumMap += (3, false);
      quorumMap += (4, false);
      q1 += (0, quorumMap);
      q1[0][1] = true;
      q1[0][2] = true;
      q1 += (1, quorumMap);
      q1[1][3] = true;
      q1[1][4] = true;
      q2 += (0, quorumMap);
      q2[0][1] = true;
      q2[0][2] = true;
      q2 += (1, quorumMap);
      q2[1][2] = true;
      q2[1][4] = true;
      q2 += (2, quorumMap);
      q2[2][1] = true;
      q2[2][4] = true;
      q2 += (3, quorumMap);
      q2[3][2] = true;
      q2[3][3] = true;
    }

    on eNext do {
      var choices : seq[(int, int, int)];
      var choice : (int, int, int);
      var canReplyP1 : bool;
      var i : int;
      var j : int;
      var k : int;
      var l : int;
      var chose : bool;
      while (i < sizeof(acceptors)) {
        j = 0;
        while (j < sizeof(ballots)) {
          if (ballots[j] >= maxBal[acceptors[i]]) {
            if (ballots[j] > maxBal[acceptors[i]] && SentP1A[ballots[j]]) {
              choices += (sizeof(choices), (0, acceptors[i], ballots[j]));
            }
            k = 0;
            chose = false;
            while (!chose && (k < sizeof(slots))) {
              l = 0;
              while (!chose && (l < sizeof(vals))) {
                if (SentP2A[acceptors[i]][ballots[j]][slots[k]][vals[l]]) {
                  if (ballots[j] > maxBal[acceptors[i]]) {
                    choices += (sizeof(choices), (1, acceptors[i], ballots[j]));
                    chose = true;
                  } else if (!hVal[acceptors[i]][ballots[j]][slots[k]][vals[l]]) {
                      choices += (sizeof(choices), (1, acceptors[i], ballots[j]));
                      chose = true;
                  }
                }
                l = l + 1;
              }
              k = k + 1;
            }
            // run RcvP3 here if possible, since it is independent of everything else
            k = 0;
            while (k < sizeof(slots)) {
              l = 0;
              while (!chose && (l < sizeof(vals))) {
                if (SentP3A[ballots[j]][slots[k]][vals[l]]) {
                  decided[acceptors[i]][slots[k]][vals[l]] = true;
                }
                l = l + 1;
              }
              k = k + 1; 
            }
          }
          j = j + 1;
        } 
        i = i + 1;
      }
      i = 0;
      while (i < sizeof(leaders)) {
        if((s[leaders[i]] in slots) && (b[leaders[i]] in ballots)) {
          // P1L - try to get elected
          if (pc[leaders[i]] == 0) {
            if (elected[leaders[i]]) {
              pc[leaders[i]] = 2;
            }
            else {
              choices += (sizeof(choices), (3, leaders[i], b[leaders[i]]));
            } 
          }
          if (pc[leaders[i]] == 1 || pc[leaders[i]] == 3) {
            // CP1L - collect responses
            if (pc[leaders[i]] == 1 && SatQ1[b[leaders[i]]]) {
              //canCollectP1 += (sizeof(canCollectP1), leaders[i]);
              choices += (sizeof(choices), (4, leaders[i], b[leaders[i]]));
            }
            // CP2L - collect responses
            if (pc[leaders[i]] == 3 && SatQ2[b[leaders[i]]]) {
              //canCollectP2 += (sizeof(canCollectP2), leaders[i]);
              choices += (sizeof(choices), (6, leaders[i], b[leaders[i]]));
            }
            if (highestP1ABallot > b[leaders[i]]) {
              // CP1L - collect responses
              if (pc[leaders[i]] == 1) {
                //canCollectP1 += (sizeof(canCollectP1), leaders[i]);
                choices += (sizeof(choices), (4, leaders[i], b[leaders[i]]));
              }
              // CP2L - collect responses
              if (pc[leaders[i]] == 3) {
                //canCollectP2 += (sizeof(canCollectP2), leaders[i]);
                choices += (sizeof(choices), (6, leaders[i], b[leaders[i]]));
              }
            }
          }
          // phase 2
          if (pc[leaders[i]] == 2) {
            // P2L
            choices += (sizeof(choices), (5, leaders[i], b[leaders[i]]));
          }
        }
        i = i + 1;
      }

      if (sizeof(choices) > 0) {
        choice = choices[choose(sizeof(choices))]; 
        if (choice.0 == 0) {
          raise eReplyP1, (acceptor=choice.1, b=choice.2);
        }
        else if (choice.0 == 1) {
          raise eReplyP2, (acceptor=choice.1, b=choice.2);
        }
        else if (choice.0 == 2) {
          raise eRecvP3, (acceptor=choice.1, b=choice.2);
        }
        else if (choice.0 == 3) {
          raise eSendP1, choice.1;
        }
        else if (choice.0 == 4) {
          raise eCollectP1, choice.1;
        }
        else if (choice.0 == 5) {
          raise eSendP2, choice.1;
        }
        else if (choice.0 == 6) {
          raise eCollectP2, choice.1;
        }
      }
    }

    on eReplyP1 do (pld : (acceptor : int, b : int)) {
      var i : int;
      var j : int;
      var k : int;
      var exists : bool;
      var forall : bool;
      print("replyP1");
      maxBal[pld.acceptor] = pld.b;
      i = 0;
      while (i < sizeof(ballots)) {
        j = 0;
        while (j < sizeof(slots)) {
          k = 0;
          while (k < sizeof(vals)) {
            SentP1B[pld.b][ballots[i]][slots[j]][vals[k]] = hVal[pld.acceptor][ballots[i]][slots[j]][vals[k]];
            k = k + 1;
          }
          j = j + 1;
        }
        i = i + 1;
      }
      p1BSenderBallot[pld.acceptor][pld.b] = true;
      // reevaluate SatQ1
      i = 0;
      exists = false;
      while (i < sizeof(q1)) {
        j = 0;
        forall = true;
        while (j < sizeof(acceptors)) {
          if (q1[i][acceptors[j]] && !p1BSenderBallot[acceptors[j]][pld.b]) {
            forall = false;
          }
          j = j + 1;
        }
        exists = exists || forall;
        i = i + 1;
      }
      SatQ1[pld.b] = exists;
      send driver, eNext;
    }

    on eReplyP2 do (pld : (acceptor : int, b : int)) {
      var i : int;
      var j : int;
      var k : int;
      var choices : seq[(int, int, int)];
      var choice : (int, int, int);
      var exists : bool;
      var forall : bool;
      print("replyP2");
      maxBal[pld.acceptor] = pld.b;
      i = 0;
      while (i < sizeof(ballots)) {
        j = 0;
        while (j < sizeof(slots)) {
          k = 0;
          while (k < sizeof(vals)) {
            if (SentP2A[pld.b][ballots[i]][slots[j]][vals[k]] &&
                !hVal[pld.acceptor][ballots[i]][slots[j]][vals[k]]) {
              choices += (sizeof(choices), (ballots[i], slots[j], vals[k]));
            }
            k = k + 1;
          }
          j = j + 1;
        }
        i = i + 1;
      }
      choice = choices[choose(sizeof(choices))];
      hVal[pld.acceptor][choice.0][choice.1][choice.2] = true;
      SentP2B[pld.b][choice.2] = true;
      p2BSenderBallot[pld.acceptor][pld.b] = true;
      // reevaluate SatQ2
      i = 0;
      exists = false;
      while (i < sizeof(q2)) {
        j = 0;
        forall = true;
        while (j < sizeof(acceptors)) {
          if (q2[i][acceptors[j]] && !p2BSenderBallot[acceptors[j]][pld.b]) {
            forall = false;
          }
          j = j + 1;
        }
        exists = exists || forall;
        i = i + 1;
      }
      SatQ2[pld.b] = exists;
      send driver, eNext;
    } 

    on eSendP1 do (leader : int) {
      b[leader] = b[leader] + M;
      print("sendP1");
      SentP1A[b[leader]] = true;
      if (b[leader] > highestP1ABallot) {
        highestP1ABallot = b[leader];
      }
      pc[leader] = 1;
      send driver, eNext;
    }

    on eCollectP1 do (leader : int) {
      var i : int;
      var j : int;
      var k : int;
      print("collectP1");
      if (highestP1ABallot <= b[leader]) {
        elected[leader] = true;
        i = 0;
        while (i < sizeof(ballots)) {
          j = 0;
          while (j < sizeof(slots)) {
            k = 0;
            while (k < sizeof(vals)) {
              if (SentP1B[b[leader]][ballots[i]][slots[j]][vals[k]]) {
                pVal[leader][ballots[i]][slots[j]][vals[k]] = true;
              }
              k = k + 1;
            }
            j = j + 1;
          }
          i = i + 1;
        }
      }
      pc[leader] = 0;
      send driver, eNext;
    }

    on eSendP2 do (leader : int) {
      var i : int;
      var j : int;
      var k : int;
      var pVals : seq[(int, int, int)];
      print("sendP2");
      // count cardinality of pVal
      i = 0;
      while (i < sizeof(ballots)) {
        j = 0;
        while (j < sizeof(slots)) {
          k = 0; 
          while (k < sizeof(vals)) { 
            if (pVal[leader][ballots[i]][slots[j]][vals[k]]) {
              pVals += (sizeof(pVals), (ballots[i], slots[j], vals[k]));
            }
            k = k + 1;
          }
          j = j + 1;
        }
        i = i + 1;
      }
      if (sizeof(pVals) == 0) {
        SentP2A[b[leader]][s[leader]][b[leader]][leader] = true;
      } else {
        i = choose(sizeof(pVals));
        SentP2A[b[leader]][pVals[i].0][pVals[i].1][pVals[i].2] = true;
      }
      pc[leader] = 3;
      send driver, eNext;
    }

    on eCollectP2 do (leader : int) {
      var i : int;
      var vvs : seq[int];
      var vv : int;
      print("collectP2");
      if (highestP1ABallot > b[leader]) {
        elected[leader] = false;
      } else {
        i = 0;
        while (i < sizeof(vals)) {
          if(SentP2B[b[leader]][vals[i]]) {
            vvs += (sizeof(vvs), vals[i]);
          }
          i = i + 1; 
        }
        // choose message
        vv = choose(vvs);
        lv[leader] = vv;
      }
      // Phase 3
      if (elected[leader]) {
        // SendP3
        SentP3A[b[leader]][s[leader]][lv[leader]] = true;
        s[leader] = s[leader] + 1;
      }
      pc[leader] = 0;
      send driver, eNext;
    }
  }
}

machine Main {
  var acceptors : seq[int];
  var leaders : seq[int];
  var slots : seq[int];
  var ballots : seq[int];
  var global  : machine;
  start state Init {
    entry {
      var i : int;
      var N : int;
      var M : int;
      var STOP : int;
      var MAXB : int;
      N = 4;
      M = 2;
      STOP = 3;
      MAXB = 5;
      i = 1;
      while (i <= N) {
        acceptors += (sizeof(acceptors), i);
        i = i + 1;
      }
      i = N + 1;
      while (i <= N + M) {
        leaders += (sizeof(leaders), i);
        i = i + 1;
      }
      i = 1; 
      while (i <= STOP) {
        slots += (sizeof(slots), i);
        i = i + 1;
      }
      i = 0;
      while (i <= MAXB) {
        ballots += (sizeof(ballots), i);
        i = i + 1;
      }
      global = new Global((driver=this, acceptSeq=acceptors, leadSeq=leaders,
                           slotSeq=slots, ballotSeq=ballots, M=M));
      raise eNext;
    }

    on eNext do {
      send global, eNext;
    }
  }
}
