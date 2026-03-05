
machine Light {
    var isOn: bool;
    
    start state Off {
        entry {
            isOn = false;
        }

        on eToggleReq do (req: tToggleReq) {
            isOn = true;
            send req.source, eStatusResp, (switchId = req.switchId, isOn = isOn);
            goto On;
        }
    }

    state On {
        entry {
            isOn = true;
        }

        on eToggleReq do (req: tToggleReq) {
            isOn = false;
            send req.source, eStatusResp, (switchId = req.switchId, isOn = isOn);
            goto Off;
        }
    }
}
