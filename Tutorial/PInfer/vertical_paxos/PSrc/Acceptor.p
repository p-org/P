machine Acceptor {
    var voted: map[tRound, tValue];
    var maxr: map[tRound, tRound];

    start state Init {
        entry {
            voted = default(map[tRound, tValue]);
            maxr = default(map[tRound, tValue]);
            goto Accepting;
        }
    }

    state Accepting {
        on eP1A do (p: tP1A) {
            if (!(p.rp in keys(maxr)) || p.round > maxr[p.rp]) {
                maxr[p.rp] = p.round;
                if (!(p.rp in voted)) {
                    send p.proposer, eP1B, (acceptor=this, round=p.round, maxr=p.rp, v=-1);
                } else {
                    send p.proposer, eP1B, (acceptor=this, round=p.round, maxr=p.rp, v=voted[p.rp]);
                }
            }
        }

        on eP2A do (p: tP2A) {
            if (p.round >= maxr[p.completed]) {
                voted[p.round] = p.value;
                maxr[p.completed] = p.round;
                send p.proposer, eP2B, (acceptor=this, round=p.round, value=p.value);
            }
        }
    }
}