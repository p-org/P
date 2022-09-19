event eNext;
event ePrepare : int;
event eDecide : int;
event eFail : int;
event eCommit : int;
event eAbort : int;
event eTAC : (tsbts : int, st : int);
event eF : int;

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
      var choicePair : (int, int);
      var choicePC : int;
      var choiceProc : int;
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
        if (canCommit) {
          choices += (sizeof(choices), (3, 1)); 
        }
        if (canAbort) {
          choices += (sizeof(choices), (4, 1));
        }
      }
      if (tManagerPC > 0) {
        choices += (sizeof(choices), (5, 0));
      }
      if (btManagerPC > 0) {
        choices += (sizeof(choices), (5, 1));
      }

      if (sizeof(choices) > 0) {
        choice = choose(sizeof(choices));
        choicePair = choices[choice];
        choicePC = choicePair.0;
        choiceProc = choicePair.1;
        
        if (choicePC == 0) {
          raise ePrepare, choiceProc;
        }
        if (choicePC == 1) {
          raise eDecide, choiceProc;
        }
        if (choicePC == 2) {
          raise eFail, choiceProc;
        }
        if (choicePC == 3) {
          raise eCommit, choiceProc;
        }
        if (choicePC == 4) {
          raise eAbort, choiceProc;
        }
        if (choicePC == 5) {
          if (choiceProc == 0) {
            if (tManagerPC == 1) {
              raise eTAC, (tsbts=0, st=2);
            } else if (tManagerPC == 2) {
              raise eTAC, (tsbts=0, st=3);
            } else {
              raise eF, 0;
            }
          }
          else {
            if (btManagerPC == 1) {
              raise eTAC, (tsbts=1, st=2);
            } else {
              raise eTAC, (tsbts=1, st=3);
            }
          }
        }
      }
    }

    on ePrepare do (p : int) {
//      print("prepare");
      rmState[p] = 1;
      send driver, eNext;
    }

    on eDecide do (p : int) {
//      print("decide");
      if (tmState == 2) {
        rmState[p] = 2;
      }
      if ((rmState[p] == 0) || tmState == 3) {
        rmState[p] = 3;
      }
      send driver, eNext;
    }

    on eFail do (p : int) {
 //     print("fail");
      if (RMMAYFAIL) {
        rmState[p] = 4;
      }
      send driver, eNext;
    }

    on eCommit do (tsbts : int) {
//      print("commit branch");
      if (tsbts == 0) {
        tManagerPC = 1;
      } else {
        btManagerPC = 1;
      }
      send driver, eNext;
    }

    on eAbort do (tsbts : int) {
//      print("abort branch");
      if (tsbts == 0) {
        tManagerPC = 2;
      } else {
        btManagerPC = 2;
      }
      send driver, eNext;
    }

    on eTAC do (pld : (tsbts : int, st : int)) {
      tmState = pld.st;
      if (pld.tsbts == 0) {
        tManagerPC = 3;
      } else {
        btManagerPC = 0;
      }
    }

    on eF do (ts : int) {
      if (TMMAYFAIL) {
        tmState = 1;
      }
      tManagerPC = 0;
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
