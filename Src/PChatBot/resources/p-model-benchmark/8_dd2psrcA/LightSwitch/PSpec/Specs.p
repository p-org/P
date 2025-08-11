spec LightToggleConsistency observes eToggleReq, eStatusResp, eSpec_LightToggleConsistency_Init {
    var expectedLightState: bool;
    var pendingToggles: set[int];

    start state Init {
        on eSpec_LightToggleConsistency_Init goto WaitForEvents with (initialState: bool) {
            expectedLightState = initialState;
        }
    }

    state WaitForEvents {
        on eToggleReq do (req: tToggleReq) {
            pendingToggles += (req.switchId);
        }

        on eStatusResp do (resp: tStatusResp) {
            expectedLightState = !expectedLightState;

            assert resp.isOn == expectedLightState, format("Light state inconsistency! Expected {0}, found {1}", expectedLightState, resp.isOn);

            assert resp.switchId in pendingToggles, format("Unexpected response for switch {0}", resp.switchId);

            pendingToggles -= (resp.switchId);
        }
    }
}


spec EventualResponse observes eToggleReq, eStatusResp {
    var pendingRequests: set[int];

    start state NoRequests {
        on eToggleReq goto PendingRequests with (req: tToggleReq) {
            pendingRequests += (req.switchId);
        }
    }

    hot state PendingRequests {
        on eStatusResp do (resp: tStatusResp) {
            assert resp.switchId in pendingRequests, format("Unexpected response for switch {0}", resp.switchId);

            pendingRequests -= (resp.switchId);

            if (sizeof(pendingRequests) == 0) {
                goto NoRequests;
            }
        }

        on eToggleReq do (req: tToggleReq) {
            pendingRequests += (req.switchId);
        }
    }
}