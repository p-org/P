machine Switch {

    var light: Light;
    var switchId: int;
    var expectedState: bool;
    var statusStr: string;

    start state Init {
        entry (input: (lightMachine: Light, id: int)) {
            light = input.lightMachine;
            switchId = input.id;
            expectedState = false; // light starts off
            goto ReadyToToggle;
        }
    }

    state ReadyToToggle {
        entry {
            send light, eToggleReq, (source = this, switchId = switchId);
            expectedState = !expectedState;
        }

        on eStatusResp do (resp: tStatusResp) {

            if(resp.isOn) {
                statusStr = "ON";
            } else {
                statusStr = "OFF";
            }
            
            print format("Switch {0}: Light is now {1}", switchId, statusStr);

        }
    }
}

