// This safety spec ensures that the value that was taught never changes (though it can be taught multiple times)
spec OneValueTaught observes eLearn {
	var decided: int;

	start state Init {
		entry {
			decided = -1;
		}

		on eLearn do (payload: (ballot: tBallot, v: tValue)) {
			assert(payload.v != -1);
			if (decided != -1) {
				assert(decided == payload.v);
			}
			decided = payload.v;
		}
	}
}

event eProgressMonitorInitialize: int;

spec Progress observes eLearn, eProgressMonitorInitialize {
	var pendingLearns: int;

	start state Init {
		on eProgressMonitorInitialize do (numLearners: int) {
			pendingLearns = numLearners;
			goto WaitForLearning;
		}
	}

	hot state WaitForLearning {
		on eLearn do (payload: (ballot: tBallot, v: tValue)) {
			pendingLearns = pendingLearns - 1;
			if (pendingLearns == 0) {
				goto LearningDone;
			}
		}
	}

	cold state LearningDone {
		ignore eLearn;
	}
}