type tBallot = int;
type tValue = int;

type tPrepareReq = (proposer: Proposer, ballot_n: tBallot, v: tValue);
event ePrepareReq: tPrepareReq;
type tPrepareRsp = (acceptor: Acceptor, promised: tBallot, v_accepted: tValue, n_accepted: tBallot);
event ePrepareRsp: tPrepareRsp;
type tAcceptReq = (proposer: Proposer, ballot_n: tBallot, v: tValue);
event eAcceptReq: tAcceptReq;
type tAcceptRsp = (acceptor: Acceptor, accepted: tBallot);
event eAcceptRsp: tAcceptRsp;

// The acceptor role in Paxos (see proposer.p for more details).
machine Acceptor {
	var n_prepared: tBallot;

	var v_accepted: tValue;
	var n_accepted: tBallot;

	start state Init {
		entry {
			n_prepared = -1;
			v_accepted = -1;
			n_accepted = -1;
			goto Accept;
		}
	}

	state Accept {
		// When we get a prepare request, we promise not to accept any prepare requests with a lower ballot number, and return any
		//  proposed value we've accepted, if any.
		on ePrepareReq do (req: tPrepareReq) {
			if (req.ballot_n > n_prepared) {
				send req.proposer, ePrepareRsp, (acceptor = this, promised = req.ballot_n, v_accepted = v_accepted, n_accepted = n_accepted);
				n_prepared = req.ballot_n;
				// print format("{0}: ({1}:{2}, {3})", this, n_accepted, v_accepted, n_prepared);
			}
			// As an optimization, we could send a NACK here to let the proposer know we got its message, allowing it to tell the difference
			//  between losing it's proposal and packet loss. It's just an optimization, so we don't do it yet.
		}

		// Once a proposer has been made the 'leader', it sends us a proposed value. To avoid accepting values from old leaders, we simply
		//  discard any messages with ballot numbers lower than the one we prepared.
		on eAcceptReq do (req: tAcceptReq) {
			if (req.ballot_n >= n_prepared) {
				v_accepted = req.v;
				n_accepted = req.ballot_n;
				// Treat accepting as "prepare, accept" the way that Lamport does in Part Time Parliament (but not in Paxos Made Simple)
				//  See https://brooker.co.za/blog/2021/11/16/paxos.html and https://stackoverflow.com/questions/29880949/contradiction-in-lamports-paxos-made-simple-paper
				n_prepared = req.ballot_n;

				send req.proposer, eAcceptRsp, (acceptor = this, accepted = req.ballot_n);
			}
			// Same story with NACKs here.
		}
	}
}