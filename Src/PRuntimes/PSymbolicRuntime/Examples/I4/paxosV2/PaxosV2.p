type mid = int;
type round = int;
type value = int;
type quorum = int;

event eNext;
event eSendA : round;
event eJoinRoundCase :  (node : mid, r: round, maxr : round, v: value);
event eProposeCase : (r: round, q: quorum, maxr : round, v : value);
event eCastVote : (n : mid, v : value, r : round);
event eDecide : (n : mid, r:round, v:value, q:quorum);

machine Global {
  var one_a : map[round, bool];
  var one_b_max_vote : map[mid, map[round, map[round, map[value, bool]]]];
  var one_b : map[mid, map[round, bool]];
  var left_rnd : map[mid, map[round, bool]];
  var proposal : map[round, map[value, bool]];
  var vote : map[mid, map[round, map[value, bool]]];
  var decision : map[mid, map[round, map[value, bool]]];
  var member : map[mid, map[quorum, bool]];
  var mids : seq[mid];
  var maxRound : int;
  var maxQuorum : int;
  var maxValue : int;
  var driver : machine;
 
  start state Init {
    entry (pld : (driver : machine, ids : seq[mid], maxRound : int, maxQuorum : int, maxValue : int)) {
      var i : int;
      var j : int;
      var k : int;
      var l : int;
      var n : mid;
      var one_b_max_vote_mid : map[round, map[round, map[value, bool]]];
      var roundValBoolMap : map[round, map[value, bool]];
      var roundBoolMap : map[round, bool];
      var valBoolMap : map[value, bool];
      var quorumBoolMap : map[quorum, bool];
      driver = pld.driver;
      mids = pld.ids; 
      maxRound = pld.maxRound;
      maxQuorum = pld.maxQuorum;
      maxValue = pld.maxValue;
      while (i < maxRound) {
        one_a[i] = false;
        proposal[i] = valBoolMap;
        j = 0;
        while (j < maxValue) {
          proposal[i][j] = false;
          j = j + 1;
        }
        i = i + 1;
      } 
      i = 0;
      j = 0;
      while (i < sizeof(mids)) {
        print("init2");
        one_b_max_vote[mids[i]] = one_b_max_vote_mid;
        one_b[mids[i]] = roundBoolMap;
        left_rnd[mids[i]] = roundBoolMap;
        vote[mids[i]] = roundValBoolMap;
        member[mids[i]] = quorumBoolMap;
        j = 0;
        while (j < maxQuorum) {
          member[mids[i]][j] = choose();
          j = j + 1;
        }
        j = 0;
        while (j < maxRound) {
          print("round iter");
          one_b_max_vote[mids[i]][j] = roundValBoolMap;
          vote[mids[i]][j] = valBoolMap;
          one_b[mids[i]][j] = false;
          left_rnd[mids[i]][j] = false;
          k = 0;
          while (k < maxRound) {
            one_b_max_vote[mids[i]][j][k] = valBoolMap;
            l = 0;
            while (l < maxValue) {
              one_b_max_vote[mids[i]][j][k][l] = false;
              vote[mids[i]][j][l] = false;
              l = l + 1;
            }
            k = k + 1;
          }
          j = j + 1;
        }
        i = i + 1;
      }
      i = 0;
      while (i < maxQuorum) {
        j = i + 1; 
        while (j < maxQuorum) {
          n = mids[choose(sizeof(mids))];
          member[n][k] = true;
          member[n][j] = true;
          j = j + 1;
        }
        i = i + 1;
      }
      decision = vote;
    }

    on eSendA do (r : round) {
      if (!one_a[r]) {
        one_a[r] = true;
        send driver, eNext;
      }
    }

    on eJoinRoundCase do (pld : (node : mid, r: round, maxr : round, v: value)) {
      var i : int;
      var j : int;
      var run : bool;
      var run1 : bool;
      var run2 : bool;
      var change : bool;
      print("read");

      if (pld.r > maxRound) {
        maxRound = pld.r;
      }
      if (pld.maxr > maxRound) {
        maxRound = pld.maxr;
      }

      // receive 1a and answer with 1b
      run = pld.r >= 0;
      if (one_a[pld.r]) {
        if (pld.r in left_rnd[pld.node]) {
          run = !left_rnd[pld.node][pld.r];
        }

        print("split runs");
        run1 = run;
        run2 = run;

        // find the maximal vote in a round less than r
        //case 1 - reserve 0 for "none" for round
        run1 = run1 && pld.maxr < 1;
        // case 2
        run2 = run2 && pld.maxr >= 1;
        run2 = run2 && pld.r > pld.maxr;
        run2 = vote[pld.node][pld.maxr][pld.v];

        print("forall");
        // forall requirement
        i = 0;
        while (i < maxRound) {
          if (pld.r > i) {
            j = 0;
            while(j < maxValue) {
              // case 1
              run1 = run1 && vote[pld.node][i][j];
              // case 2
              run2 = run2 && (i < pld.maxr || !vote[pld.node][i][j]);
              j = j + 1;
            }
          }
          i = i + 1;
        }

        if (run1 || run2) {
          // send the 1b message

          if (!one_b_max_vote[pld.node][pld.r][pld.maxr][pld.v]) {
            print("update one_b_max_vote");
            change = true;
            one_b_max_vote[pld.node][pld.r][pld.maxr][pld.v] = true;
          }

          if (!one_b[pld.node][pld.r]) {
            print("update one_b");
            change = true;
            one_b[pld.node][pld.r] = true;
          }

          print(format("one_b_max_vote {0}%s", one_b_max_vote[pld.node][pld.r][pld.maxr][pld.v]));

          i = 0;
          while (i < maxRound) {
            if (pld.r > i) {
              if (!left_rnd[pld.node][i]) {
                print("update left_rnd");
                change = true;
                left_rnd[pld.node][i] = true;
              }
            }
            i = i + 1;
          }
        }
      }
      if (change) {
        send driver, eNext;
      }
    }

    on eProposeCase do (pld : (r: round, q: quorum, maxr : round, v : value)) {
      var run : bool;
      var i : int;
      var j : int;
      var k : int;
      var run1 : bool;
      var run2 : bool;
      var exists : bool;
      var cond : bool;
      var change : bool;
      print("propose");

      // receive a quorum of 1b's and send a 2a (proposal)
      run = true;
      // require ~proposal(r,V);
      while (i < maxRound) {
        // need to add this in V1
        j = 0;
        while (j < maxValue) {
          run = run && (!proposal[pld.r][j]);
          j = j + 1;
        }
        i = i + 1;
      }

      i = 0;
      while (run && (i < sizeof(mids))) {
        if (member[mids[i]][pld.q]) {
          // note: this isn't correct in V1
          run = run && one_b[mids[i]][pld.r];
        }
        i = i + 1;
      }

      run1 = run;
      run2 = run;

      // find the maximal max_vote in the quorum
      //case 1 - reserve 0 for "none" for round
      run1 = run1 && pld.maxr < 1;
      // case 2
      run2 = run2 && pld.maxr >= 1;
      //exists
      run2 = run2 && pld.maxr > pld.r;
      if (run2) {
        i = 0;
        exists = false;
        while (i < sizeof(mids)) {
          if (member[mids[i]][pld.q]) {
            if (i in vote) {
              exists = exists || vote[i][pld.maxr][pld.v];
            }
          }
          i = i + 1;
        }
        run2 = exists;
      }

      print("run2");

      //forall 
      i = 0;
      j = 0;
      k = 0;
      while (i < sizeof(mids)) {
        j = 0;
        while (j < maxRound) {
          k = 0;
          while (k < maxValue) {
            cond = true;
            // member(N, q)
            if (!member[mids[i]][pld.q]) {
              cond = false;
            }
            // ~le(r,MAXR)
            cond = cond && pld.r > j;
            // vote(N,MAXR,V)
            cond = cond && vote[mids[i]][j][k];
            if (run1) {
              run1 = run1 && !cond;
            }
            if (run2) {
              run2 = run2 && (!cond || (pld.maxr > j));
            }
            k = k + 1;
          }
          j = j + 1;
        }
        i = i + 1;
      }

      change = false;
      if (run1 || run2) {
        if (!proposal[pld.r][pld.v]) {
          print("update proposal");
          change = true;
          proposal[pld.r][pld.v] = true;
        }
      }
      if (change) {
        send driver, eNext;
      }
    } 

    on eCastVote do (pld : (n:mid, v:value, r:round)) {
      var run : bool;
      var change : bool;
      var vote_n_r : map[value, bool];
      var vote_n : map[round, map[value, bool]];
      print("cast vote");
      run = true;
      // ~left_rnd(n,r)
      run = !left_rnd[pld.n][pld.r];
      // proposal(r, v)
      run = run && proposal[pld.r][pld.v];

      if (run) {
        if (!vote[pld.n][pld.r][pld.v]) {
          print("update vote");
          change = true;
          vote[pld.n][pld.r][pld.v] = true;
        }
      }
      if (change) {
        send driver, eNext;
      }
    }

    on eDecide do (pld : (n:mid, r:round, v:value, q:quorum)) {
      var i : int;
      var change : bool; 
      var run : bool;
      print("decide");
      run = true;
      // forall N . member(N, q) -> vote(N, r, v);
      while (i < sizeof(mids)) {
        run = run && vote[mids[i]][pld.r][pld.v];
        i = i + 1;
      }

      if (run) {
        if (!decision[pld.n][pld.r][pld.v]) {
          change = true;
          print("update decision");
          decision[pld.n][pld.r][pld.v] = true;
        }
        // check
        i = 0;
        while (i < maxValue) {
          if (i != pld.v) {
            assert(!decision[pld.n][pld.r][i]);
          } 
          i = i + 1;
        }
      }
      if (change) {
        send driver, eNext;
      }

    }
  }

}

machine Main {
  var global : machine;
  var mids : seq[mid];
  var maxRounds : int;
  var maxVal : int;
  var maxQuorum : int;
  start state Init {
    entry {
      var i : int;
      i = 0;
      maxRounds = 3;
      maxVal = 3;
      maxQuorum = 2;
      while (i < 6) {
        mids += (i, i);
        i = i + 1;
      } 
      global = new Global((driver=this, ids=mids, maxRound = maxRounds, maxQuorum = maxQuorum, maxValue = maxVal));
      raise eNext;
    }

    on eNext do {
      var choice : int;
      var nodeChoice : int;
      var quorumChoice : int;
      var roundChoice : int;
      var maxRoundChoice : int;
      var valChoice : int;
      choice = choose(5);
      nodeChoice = choose(mids);
      quorumChoice = choose(maxQuorum);
      roundChoice = choose(maxRounds);
      maxRoundChoice = choose(maxRounds);
      valChoice = choose(maxVal);
      if(choice == 0) {
        send global, eSendA, roundChoice;
      } else if (choice == 1){
        send global, eJoinRoundCase, (node=nodeChoice,r=roundChoice,maxr=maxRoundChoice,v=valChoice);
      } else if (choice == 2) {
        send global, eProposeCase, (r=roundChoice,q=quorumChoice,maxr=maxRoundChoice,v=valChoice);
      } else if (choice == 3) {
        send global, eCastVote, (n=nodeChoice,v=valChoice,r=roundChoice);
      } else {
        send global, eDecide, (n=nodeChoice,r=roundChoice,v=valChoice,q=quorumChoice);
      }
    }
  }
}
