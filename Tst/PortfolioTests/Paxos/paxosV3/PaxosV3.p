type mid = int;
type round = int;
type value = int;
type quorum = int;

event eNext;
event eSendA;
event eJoinRoundCase;
event eProposeCase;
event eCastVote;
event eDecide;

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
        k = 0;
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

    on eSendA do {
      var i : int;
      var j : int;
      var choices : seq[int];
      print("send");
      while (i < maxRound) {
        if (!one_a[i]) {
          choices += (j, i);
          j = j + 1;
        }
        i = i + 1;
      }
      if (sizeof(choices) > 0) {
        one_a[choose(choices)] = true;
        send driver, eNext;
      }
    }

    on eJoinRoundCase do {
      var i : int;
      var j : int;
      var k : int;
      var roundNodeChoices : seq[(r:round, n:mid)];
      var nodeChoices : seq[int];
      var maxRoundChoices : seq[int];
      var valChoices : seq[int];
      var valChoices2 : seq[int];
      var r : int;
      var n : int;
      var maxr : int;
      var v : int;
      print("join");
      while (i < maxRound) {
        if (one_a[i]) {
          j = 0;
          while (j < sizeof(mids)) {
            if(!left_rnd[mids[j]][i]) {
              roundNodeChoices += (k, (r=i, n=mids[j]));
            }
            j = j + 1;
          }
        }
        i = i + 1;
      }
      if (sizeof(roundNodeChoices) > 0) {
        j = 0;
        k = 0;
        i = choose(sizeof(roundNodeChoices));
        r = roundNodeChoices[i].r;
        n = roundNodeChoices[i].n;
        i = 0;
        while (i < r) {
          j = 0;
          while (j < maxValue) {
            if (vote[n][i][j]) {
              maxr = i;
              k = k + 1;
            }
            j = j + 1;
          }
          i = i + 1;
        }
        i = 0;
        j = 0;
        while (i < maxValue) {
          if (vote[n][maxr][i]) {
            valChoices += (j, i);
            j = j + 1;
          }
          i = i + 1;
        }
        if (sizeof(valChoices) > 0) {
          i = 0;
          j = 0;
          while (i < sizeof(valChoices)) {
            if(!one_b_max_vote[n][r][maxr][valChoices[i]] || !one_b[n][r]) {
              valChoices2 += (j, valChoices[i]);
              j = j + 1;
            }
            i = i + 1;
          }
        } else {
          i = 0;
          j = 0;
          while (i < maxValue) {
            if(!one_b_max_vote[n][r][maxr][i] || !one_b[n][r]) {
              valChoices2 += (j, i);
              j = j + 1;
            }
            i = i + 1;
          }
        }
        if (sizeof(valChoices2) > 0) {
          v = choose(valChoices2);
          one_b_max_vote[n][r][maxr][v] = true;
          one_b[n][r] = true;
          i = 0;
          while (i < r) {
            left_rnd[n][i] = true;
            i = i + 1;
          }
          send driver, eNext;
       }
      }
    }

    on eProposeCase do {
      var i : int;
      var j : int;
      var k : int;
      var l : int;
      var trackProposal : bool;
      var trackImpl : bool;
      var roundQuorumChoices : seq[(r:round, q:quorum)];
      var valChoices : seq[int];
      var valChoices2 : seq[int];
      var trackQuorum : bool;
      var r : int;
      var q : int;
      var maxr : int;
      var v : int;

      print("propose");
      l = 0;
      while (i < maxRound) {
        j = 0;
        trackProposal = false;
        while (j < maxValue) {
          if (proposal[i][j]) {
            trackProposal = true;
          }
          j = j + 1;
        }
        if (!trackProposal) {
          j = 0;
          while (j < maxQuorum) {
            trackImpl = true;
            k = 0;
            while (k < sizeof(mids)) {
              if (member[mids[k]][j] && !one_b[mids[k]][i]) {
                trackImpl = false;
              }
              k = k + 1;
            }
            if (trackImpl) {
              roundQuorumChoices += (l, (r=i, q=j));
              l = l + 1;
            }
            j = j + 1;
          }
        }
        i = i + 1;
      }

      if (sizeof(roundQuorumChoices) > 0) {
        i = choose(sizeof(roundQuorumChoices));
        r = roundQuorumChoices[i].r;
        q = roundQuorumChoices[i].q;
        i = 0;
        k = 0;
        while (i < sizeof(mids)) {
          j = 0;
          while (j < r) {
            k = 0;
            while (k < maxValue) {
              if (member[mids[i]][q] && vote[mids[i]][j][k]) {
                maxr = j;
              }
              k = k + 1;
            }
            j = j + 1;
          }
          i = i + 1;
        }

        i = 0;
        k = 0;
        while (i < sizeof(mids)) {
          j = 0;
          while (j < maxValue) {
            if (vote[mids[i]][maxr][j]) {
              valChoices += (k, j);
              k = k + 1;
            }
            j = j + 1;
          }
          i = i + 1;
        }
        if (sizeof(valChoices) > 0) {
          i = 0;
          j = 0;
          while (i < sizeof(valChoices)) {
            if(!proposal[r][valChoices[i]]) {
              valChoices2 += (j, valChoices[i]);
              j = j + 1;
            }
            i = i + 1;
          }
        } else {
          i = 0;
          j = 0;
          while (i < maxValue) {
            if(!proposal[r][i]) {
              valChoices2 += (j, i);
              j = j + 1;
            }
            i = i + 1;
          }
        }
        if (sizeof(valChoices2) > 0) {
          proposal[r][choose(valChoices2)] = true;
          send driver, eNext;
        }
      }

    }

    on eCastVote do {
      var nvrChoice : seq[(n:mid, v:value, r:round)];
      var nodeChoice : seq[int];
      var i : int;
      var j : int;
      var k : int;
      var l : int;
      var n : mid;
      var v : value;
      var r : round;
      print("cast");
      while (i < maxRound) {
        j = 0;
        while (j < sizeof(mids)) {
          k = 0;
          while (k < maxValue) {
            if ((!left_rnd[mids[j]][i]) && proposal[i][k] && !vote[mids[j]][i][k]) {
              nvrChoice += (l, (n=mids[j], v=k, r=i));
              l = l + 1;
            }
            k = k + 1;
          }
          j = j + 1;
        }
        i = i + 1;
      }
      if (sizeof(nvrChoice) > 0) {
        i = choose(sizeof(nvrChoice));
        n = nvrChoice[i].n;
        v = nvrChoice[i].v;
        r = nvrChoice[i].v;
        vote[n][r][v] = true;
        send driver, eNext;
      }
    }

    on eDecide do {
      var rvqChoice : seq[(r:round, v:value, q:quorum)];
      var nodeChoice : seq[int];
      var i : int;
      var j : int;
      var k : int;
      var l : int;
      var m : int;
      var v : value;
      var r : round;
      var q : quorum;
      var n : mid;
      print("decide");
      while (i < sizeof(mids)) {
        j = 0;
        while (j < maxRound) {
          k = 0;
          while (k < maxValue) {
            l = 0;
            while (l < maxQuorum) {
              if (vote[mids[i]][j][k] || !member[mids[i]][l]) {
                rvqChoice += (m, (r=j, v=k, q=l));
                m = m + 1;
              }
              l = l + 1;
            }
            k = k + 1;
          }
          j = j + 1;
        }
        i = i + 1;
      }
      if (sizeof(rvqChoice) > 0) {
        i = choose(sizeof(rvqChoice));
        r = rvqChoice[i].r;
        v = rvqChoice[i].v;
        q = rvqChoice[i].q;
        i = 0;
        j = 0;
        while (i < sizeof(mids)) {
          if (!decision[n][r][v]) {
            nodeChoice += (j, i);
            j = j + 1;
          }
          i = i + 1;
        }
        if (sizeof(nodeChoice) > 0) {
          n = choose(nodeChoice);
          decision[n][r][v] = true;
          send driver, eNext;
          // check
          i = 0;
          while (i < maxValue) {
            if (i != v) {
              assert(!decision[n][r][i]);
            }
            i = i + 1;
          }
        }
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
      if(choice == 0) {
        send global, eSendA;
      } else if (choice == 1){
        send global, eJoinRoundCase;
      } else if (choice == 2) {
        send global, eProposeCase;
      } else if (choice == 3) {
        send global, eCastVote;
      } else {
        send global, eDecide;
      }
    }
  }
}
