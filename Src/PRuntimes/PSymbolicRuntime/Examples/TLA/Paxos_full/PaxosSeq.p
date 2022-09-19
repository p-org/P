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
  var slots : seq[int];
  var ballots : seq[int];
  // acceptor
  var maxBal : map[int, int];
  var hVal : map[int, seq[(int, int, int)]];
  var decided : map[int, map[int, set[int]]];
  // leader
  var b : map[int, int];
  var s : map[int, int];
  var elected : map[int, bool];
  var lv : map[int, int];
  var pVal : map[int, seq[(int, int, int)]];
  var pc : map[int, int];
  // quora
  var q1 : seq[seq[int]];
  var q2 : seq[seq[int]];
  // globals
  var SentP1A : set[int];
  var SentP2A : map[int, seq[(int, int, int)]];
  var SentP3A : map[int, seq[(slot : int, dcd : int)]];
  var SentP1B : map[int, seq[(int, int, int)]];
  var SentP2B : map[int, seq[int]];
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
      var emptySet : set[int];
      var emptySeq : seq[int];
      var emptySeq2 : seq[(int, int, int)];
      var decidedMap : map[int, set[int]];
      var sentBallots : map[int, bool];
      var sentBallots2 : map[int, bool];
      var sent3As : seq[(slot:int,dcd:int)];
      var valSet : seq[(int, int, int)];
      var q10 : seq[int];
      var q11 : seq[int];
      var q20 : seq[int];
      var q21 : seq[int];
      var q22 : seq[int];
      var q23 : seq[int];
      M = pld.M;
      driver = pld.driver;
      highestP1ABallot = -1;
      acceptors = pld.acceptSeq;
      leaders = pld.leadSeq;
      slots = pld.slotSeq; 
      ballots = pld.ballotSeq;
      valSet += (0, (-1, -1, -1));
      // acceptor initialization
      while (i < sizeof(slots)) {
        decidedMap += (slots[i], emptySet); 
        i = i + 1;
      }
      i = 0;
      while (i < sizeof(acceptors)) {
        maxBal += (acceptors[i], -1);
        hVal += (acceptors[i], valSet);
        decided += (acceptors[i], decidedMap);
        i = i + 1;
      }
      // leader initialization
      i = 0;
      while (i < sizeof(leaders)) {
        b += (leaders[i], leaders[i] - sizeof(acceptors));
        s += (leaders[i], 1);
        elected += (leaders[i], false);
        lv += (leaders[i], -1);
        pVal += (leaders[i], valSet);
        pc += (leaders[i], 0);
        i = i + 1;
      }
      // sent acc msgs
      i = 0;
      SentP1A = emptySet;
      while (i < sizeof(ballots) + M) {
        SentP2A += (i, emptySeq2);
        SentP3A += (i, sent3As);
        SentP1B += (i, emptySeq2);
        SentP2B += (i, emptySeq);
        SatQ1[i] = false;
        SatQ2[i] = false;
        i = i + 1;
      }
      i = 0;
      while (i < sizeof(acceptors)) {
        p1BSenderBallot[acceptors[i]] = SatQ1;
        p2BSenderBallot[acceptors[i]] = SatQ1;
        i = i + 1;
      }
      q10 += (0, 1);
      q10 += (1, 2);
      q1 += (0, q10);
      q11 += (0, 3);
      q11 += (1, 4);
      q1 += (1, q11);
      q20 += (0, 1);
      q20 += (1, 3);
      q2 += (0, q20);
      q21 += (0, 2);
      q21 += (1, 4);
      q2 += (1, q21);
      q22 += (0, 1);
      q22 += (1, 4);
      q2 += (2, q22);
      q23 += (0, 2);
      q23 += (1, 3);
      q2 += (3, q23);
    }

    on eNext do {
      var choices : seq[(int, int, int)];
      var choice : (int, int, int);
      var canReplyP1 : bool;
      var i : int;
      var j : int;
      var k : int;
      var bLeader : int;
      var leader : int;
      var choicePC : int;
      var choice1 : int;
      while (i < sizeof(acceptors)) {
        j = 0;
        while (j < sizeof(ballots)) {
          if (ballots[j] >= maxBal[acceptors[i]]) {
            if (ballots[j] > maxBal[acceptors[i]] && ballots[j] in SentP1A) {
              choices += (sizeof(choices), (0, acceptors[i], ballots[j]));
            }
            if (sizeof(SentP2A[ballots[j]]) > 0) {
              if (ballots[j] > maxBal[acceptors[i]]) {
                choices += (sizeof(choices), (1, acceptors[i], ballots[j]));
              } else {
                k = 0;
                while (k < sizeof(SentP2A[ballots[j]])) {
                  if (!(SentP2A[ballots[j]][k] in hVal[acceptors[i]])) {
                    choices += (sizeof(choices), (1, acceptors[i], ballots[j]));
                  }
                  k = k + 1;
                }
              }
            }
            if (sizeof(SentP3A[ballots[j]]) > 0) {
              //canRecvP3 += (sizeof(canRecvP3), (acceptors[i], ballots[j]));
              k = 0;
              while (k < sizeof(SentP3A[ballots[j]])) {
                if (!(SentP3A[ballots[j]][k].dcd in
                      decided[acceptors[i]][SentP3A[ballots[j]][k].slot])) {
                  // just run RcvP3 here, since it is independent of everything else
                  //decided[acceptors[i]][SentP3A[ballots[j]][k].slot] += (SentP3A[ballots[j]][k].dcd);
                  //assert(sizeof(decided[acceptors[i]][SentP3A[ballots[j]][k].slot]) == 1);
                  choices += (sizeof(choices), (2, acceptors[i], ballots[j]));
                }
                k = k + 1;
              }
            }
          }
          j = j + 1;
        } 
        i = i + 1;
      }
      i = 0;
      while (i < sizeof(leaders)) {
        bLeader = b[leaders[i]];
        leader = leaders[i];
        if((pc[leader] == 0 && ((s[leader] in slots) && (bLeader in ballots))) ||
           !(pc[leader] == 0)) {
          if (pc[leader] == 0) {
            pc[leader] = 1;
          }
          // P1L - try to get elected
          if (pc[leader] == 1) {
            if (elected[leader]) {
              pc[leader] = 3;
            }
            else {
              choices += (sizeof(choices), (3, leader, bLeader));
            } 
          }
          if (pc[leader] == 2 || pc[leader] == 4) {
            // CP1L - collect responses
            if (pc[leader] == 2 && SatQ1[bLeader]) {
              //canCollectP1 += (sizeof(canCollectP1), leader);
              choices += (sizeof(choices), (4, leader, bLeader));
            }
            // CP2L - collect responses
            if (pc[leader] == 4 && SatQ2[bLeader]) {
              //canCollectP2 += (sizeof(canCollectP2), leader);
              choices += (sizeof(choices), (6, leader, bLeader));
            }
            if (highestP1ABallot > bLeader) {
              // CP1L - collect responses
              if (pc[leader] == 2) {
                //canCollectP1 += (sizeof(canCollectP1), leader);
                choices += (sizeof(choices), (4, leader, bLeader));
              }
              // CP2L - collect responses
              if (pc[leader] == 4) {
                //canCollectP2 += (sizeof(canCollectP2), leader);
                choices += (sizeof(choices), (6, leader, bLeader));
              }
            }
          }
          // phase 2
          if (pc[leader] == 3) {
            // P2L
            choices += (sizeof(choices), (5, leader, bLeader));
          }
        }
        i = i + 1;
      }

      if (sizeof(choices) > 0) {
        choice = choices[choose(sizeof(choices))]; 
        choicePC = choice.0;
        choice1 = choice.1;
        if (choicePC == 0) {
          raise eReplyP1, (acceptor=choice1, b=choice.2);
        }
        else if (choicePC == 1) {
          raise eReplyP2, (acceptor=choice1, b=choice.2);
        }
        else if (choicePC == 2) {
          raise eRecvP3, (acceptor=choice1, b=choice.2);
        }
        else if (choicePC == 3) {
          raise eSendP1, choice1;
        }
        else if (choicePC == 4) {
          raise eCollectP1, choice1;
        }
        else if (choicePC == 5) {
          raise eSendP2, choice1;
        }
        else if (choicePC == 6) {
          raise eCollectP2, choice1;
        }
      }
    }

    on eReplyP1 do (pld : (acceptor : int, b : int)) {
      var i : int;
      var j : int;
      var exists : bool;
      var forall : bool;
      var pldB : int;
      var pldAcceptor : int;
      pldB = pld.b;
      pldAcceptor = pld.acceptor;
      print("replyP1");
      maxBal[pldAcceptor] = pldB;
      while (i < sizeof(hVal[pld.acceptor])) {
        if (!(hVal[pldAcceptor][i] in SentP1B[pldB] )) {
          SentP1B[pldB] += (sizeof(SentP1B[pldB]), hVal[pldAcceptor][i]);
        }
        p1BSenderBallot[pldAcceptor][pldB] = true;
        i = i + 1;
      }
      // reevaluate SatQ1
      i = 0;
      exists = false;
      while (i < sizeof(q1)) {
        j = 0;
        forall = true;
        while (j < sizeof(q1[i])) {
          if (!p1BSenderBallot[q1[i][j]][pldB]) {
            forall = false;
          }
          j = j + 1;
        }
        exists = exists || forall;
        i = i + 1;
      }
      SatQ1[pldB] = exists;
      send driver, eNext;
    }

    on eReplyP2 do (pld : (acceptor : int, b : int)) {
      var i : int;
      var j : int;
      var choices : seq[(int, int, int)];
      var choice : (int, int, int);
      var exists : bool;
      var forall : bool;
      var pldB : int;
      var pldAcceptor : int;
      pldB = pld.b;
      pldAcceptor = pld.acceptor;
      //print("replyP2");
      maxBal[pldAcceptor] = pldB;
      i = 0;
      while (i < sizeof(SentP2A[pldB])) {
        if (!(SentP2A[pldB][i] in hVal[pldAcceptor])) {
          choices += (sizeof(choices), SentP2A[pldB][i]);
        }
        i = i + 1;
      }
      choice = choices[choose(sizeof(choices))];
      if (!(choice in hVal[pldAcceptor])) {
        hVal[pldAcceptor] += (sizeof(hVal[pldAcceptor]), choice);
      }
      if (!(choice.2 in SentP2B[pldB])) {
        SentP2B[pldB] += (sizeof(SentP2B[pldB]), choice.2);
      }
      p2BSenderBallot[pldAcceptor][pldB] = true;
      // reevaluate SatQ2
      i = 0;
      exists = false;
      while (i < sizeof(q2)) {
        j = 0;
        forall = true;
        while (j < sizeof(q2[i])) {
          if (!p2BSenderBallot[q2[i][j]][pldB]) {
            forall = false;
          }
          j = j + 1;
        }
        exists = exists || forall;
        i = i + 1;
      }
      SatQ2[pldB] = exists;
      send driver, eNext;
    } 

    on eRecvP3 do (pld : (acceptor : int, b : int)) {
      var choices : seq[(slot : int, dcd : int)];
      var i : int;
      var choice : (slot : int, dcd : int);
      var pldB : int;
      var pldAcceptor : int;
      var sentP3AElt : (slot : int, dcd : int);
      var slot : int;
      pldB = pld.b;
      pldAcceptor = pld.acceptor;
      print("recvP3");
      maxBal[pldAcceptor] = pldB;
      i = 0;
      while (i < sizeof(SentP3A[pldB])) {
        sentP3AElt = SentP3A[pldB][i];
        if (!(sentP3AElt.dcd in
              decided[pldAcceptor][sentP3AElt.slot])) {
          choices += (sizeof(choices), sentP3AElt);
        }
        i = i + 1;
      }
      choice = choices[choose(sizeof(choices))];
      slot = choice.slot;
      decided[pldAcceptor][slot] += (choice.dcd);
      assert(sizeof(decided[pldAcceptor][slot]) == 1);
      send driver, eNext;
    } 

    on eSendP1 do (leader : int) {
      b[leader] = b[leader] + M;
      print("sendP1");
      SentP1A += (b[leader]);//(sizeof(SentP1A), b[leader]);
      if ((b[leader] > highestP1ABallot) && b[leader] <= ballots[sizeof(ballots) - 1]) {
        highestP1ABallot = b[leader];
      }
      pc[leader] = 2;
      send driver, eNext;
    }

    on eCollectP1 do (leader : int) {
      var i : int;
      var ballot : int;
      ballot = b[leader];
      print("collectP1");
      if (highestP1ABallot <= ballot) {
        elected[leader] = true;
        while (i < sizeof(SentP1B[ballot])) {
          if (!(SentP1B[ballot][i] in pVal[leader])) {
            pVal[leader] += (sizeof(pVal[leader]), SentP1B[ballot][i]);
          }
          i = i + 1;
        }
      }
      pc[leader] = 1;
      send driver, eNext;
    }

    on eSendP2 do (leader : int) {
      var i : int;
      var pVals : seq[(int, int, int)];
      var ballot : int;
      var slot : int;
      ballot = b[leader];
      slot = s[leader];
      print("sendP2");
      // count cardinality of pVal
      while (i < sizeof(pVal[leader])) {
        if (pVal[leader][i].0 == slot) {
          pVals += (sizeof(pVals), pVal[leader][i]); //(sizeof(pVals), pVal[i]);
        }
        i = i + 1;
      }
      if (sizeof(pVals) == 0) {
        if (!((slot, ballot, leader) in SentP2A[ballot])) {
          SentP2A[ballot] += (sizeof(SentP2A[ballot]), (slot, ballot, leader));
        }
      } else {
        i = choose(sizeof(pVals));
        if (!(pVals[i] in SentP2A[ballot])) {
          SentP2A[ballot] += (sizeof(SentP2A[ballot]), (pVals[i]));
        }
      }
      pc[leader] = 4;
      send driver, eNext;
    }

    on eCollectP2 do (leader : int) {
      var vv : int;
      var ballot : int;
      var slot : int;
      ballot = b[leader];
      slot = s[leader];
      print("collectP2");
      if (highestP1ABallot > ballot) {
        elected[leader] = false;
      } else {
        // choose message
        vv = SentP2B[ballot][choose(sizeof(SentP2B[ballot]))];
        lv[leader] = vv;
      }
      print("phase 3");
      // Phase 3
      if (elected[leader]) {
        // SendP3
        if (!((slot=slot, dcd=lv[leader]) in SentP3A[ballot])) {
          SentP3A[ballot] += (sizeof(SentP3A[ballot]), (slot=slot, dcd=lv[leader]));
        }
        s[leader] = slot + 1;
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
      MAXB = 3;
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
