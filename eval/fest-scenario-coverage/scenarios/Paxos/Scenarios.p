/**************************************************************************
 * Fest scenario-coverage examples for the SingleDecreePaxos model.
 * See ClientServer/PSpec/Scenarios.p for what a `scenario` is.
 *
 * A proposer that wins its round teaches the decided value to the learners
 * via eLearn(ballot, v). The value may be taught more than once (multiple
 * proposers / acceptors), but ballots are small (= proposer_id).
 **************************************************************************/

// COMMON: a value is learned at least once.
scenario ValueLearned observes eLearn {
  start hot state Init {
    on eLearn goto Done;
  }
  cold state Done {
    on eLearn do { }
  }
}

// RARE-ish: a value is taught at least twice in the same schedule
// (e.g. two acceptors/proposers drive the learner). Diversity payoff.
scenario TwoLearns observes eLearn {
  start hot state Init {
    on eLearn goto One;
  }
  hot state One {
    on eLearn goto Done;
  }
  cold state Done {
    on eLearn do { }
  }
}

// PAYLOAD / IMPOSSIBLE: a value learned at an astronomically high ballot.
// Ballots equal a proposer's small id, so this is never satisfied -> exercises
// partial-coverage tracking (best progress 1/2) and the liveness exemption.
scenario ImpossibleHighBallot observes eLearn {
  start hot state Init {
    on eLearn do (l: (ballot: tBallot, v: tValue)) {
      if (l.ballot > 100000) { goto Done; }
    }
  }
  cold state Done {
    on eLearn do { }
  }
}
