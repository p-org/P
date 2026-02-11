event eLearn: (ballot: tBallot, v: tValue);

// The learner role in Paxos (see proposer.p for more details).
machine Learner {
	var learned_value: tValue;

	start state Init {
		entry {
			learned_value = -1;
			goto Learn;
		}
	}

	state Learn {
		on eLearn do (payload: (ballot: tBallot, v: tValue)) {
			assert(payload.v != -1);
			// This check is a belt-and-braces with the spec, but it's a useful piece of sanity
			assert((learned_value == -1) || (learned_value == payload.v));
			learned_value = payload.v;
		}
	}
}