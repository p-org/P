event eKill;
machine Node {
    start state WaitForPing {
        on ePing do (req: (fd: FailureDetector, trial: int)) {
            UnReliableSend(req.fd, ePong, (node = this, trial = req.trial));
        }

        on eKill do {
            raise halt;
        }
    }
}