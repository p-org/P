event eNext;
event ePrepare : int;
event eDecide : int;
event eFail : int;
event eCommit : int;
event eAbort : int;

machine TransactionCommit {
  var RM : seq[int];
  // 0 - working, 1 - prepared, 2 - committed, 3 - aborted, 4 - failed
  var rmState : seq[int];
  // 0 - init, 1 - hidden, 2 - commit, 3 - abort
  var tmState : int;
  var RMMAYFAIL : bool;
  var TMMAYFAIL : bool;
  var driver : machine;
  var tManagerPC : int;
  var btManagerPC : int;

  start state Init {
    entry (pld : (driver : machine, RM : int, RMMAYFAIL : bool, TMMAYFAIL : bool)) {
      var i : int;
      i = 0;
      while (i < pld.RM) {
        RM += (sizeof(RM), i);
        rmState += (sizeof(rmState), 0);
        i = i + 1;
      }
      RMMAYFAIL = pld.RMMAYFAIL; 
      TMMAYFAIL = pld.TMMAYFAIL;
      driver = pld.driver;
    }
    
    on eNext do {
      var choices : seq[(int, int)];
      var choice : int;
      var i : int;
      var allPrepared : bool;
      var atLeastOneCommit : bool;
      var atLeastOneAbortOrFail : bool;
      var canCommit : bool;
      var canAbort : bool;
      var next : bool;
      var last : int;
      allPrepared = true; 
      i = 0;
      while (i < sizeof(RM)) {
        // rmState is "working" or "prepared"
        if (rmState[RM[i]] < 2) {
          // prepare
          if (rmState[RM[i]] == 0) {
            allPrepared = false;
            choices += (sizeof(choices), (0, RM[i]));
          }
          // decide
          if ((tmState == 2) || (rmState[RM[i]] == 0 || tmState == 3)) {
            choices += (sizeof(choices), (1, RM[i]));
          }
          // fail
          if (RMMAYFAIL) {
            choices += (sizeof(choices), (2, RM[i]));
          }
        } else {
          allPrepared = false;
          if (rmState[RM[i]] == 2) {
            atLeastOneCommit = true;
          } else {
            atLeastOneAbortOrFail = true;
          }
        }
        i = i + 1;
      }
      canCommit = allPrepared || atLeastOneCommit;
      canAbort = atLeastOneAbortOrFail && (!atLeastOneCommit); 
      last = tmState;
      if (tManagerPC == 0) {
        if (canCommit && tmState != 2) {
          choices += (sizeof(choices), (3, 0)); 
        }
        if (canAbort && tmState != 3) {
          choices += (sizeof(choices), (4, 0));
        }
      }
      if (btManagerPC == 0) {
        if (canCommit && tmState == 1) {
          choices += (sizeof(choices), (3, 1)); 
        }
        if (canAbort && tmState == 1) {
          choices += (sizeof(choices), (4, 1));
        }
      }

      if (sizeof(choices) > 0) {
        choice = choose(sizeof(choices));
        if (choices[choice].0 == 0) {
          raise ePrepare, choices[choice].1;
        }
        if (choices[choice].0 == 1) {
          raise eDecide, choices[choice].1;
        }
        if (choices[choice].0 == 2) {
          raise eFail, choices[choice].1;
        }
        if (choices[choice].0 == 3) {
          raise eCommit, choices[choice].1;
        }
        if (choices[choice].0 == 4) {
          raise eAbort, choices[choice].1;
        }
      }
    }

    on ePrepare do (p : int) {
      print("prepare");
      rmState[p] = 1;
      send driver, eNext;
    }

    on eDecide do (p : int) {
      print("decide");
      if (tmState == 2) {
        rmState[p] = 2;
      }
      if ((rmState[p] == 0) || tmState == 3) {
        rmState[p] = 3;
      }
      send driver, eNext;
    }

    on eFail do (p : int) {
      print("fail");
      if (RMMAYFAIL) {
        rmState[p] = 4;
      }
      send driver, eNext;
    }

    on eCommit do (tsbts : int) {
      print("commit");
      tmState = 2;
      if (tsbts == 0) {
        if (TMMAYFAIL) {
          tmState = 1;
        }
        tManagerPC = 1;
      } else {
        btManagerPC = 1;
      }
      send driver, eNext;
    }

    on eAbort do (tsbts : int) {
      print("abort");
      tmState = 3;
      if (tsbts == 0) {
        if (TMMAYFAIL) {
          tmState = 1;
        }
        tManagerPC = 1;
      } else {
        btManagerPC = 1;
      }
      send driver, eNext;
    }
  }
}

machine Main {
  var global : machine;
  start state Init {
    entry {
      global = new TransactionCommit((driver=this, RM=5, RMMAYFAIL=true, TMMAYFAIL=true));
      send global, eNext;
    }

    on eNext do {
      send global, eNext;
    }
  } 
}
