event eSpec_LightToggleConsistency_Init: bool;
type tToggleReq = (source: Switch, switchId: int);
type tStatusResp = (switchId: int, isOn: bool);
event eToggleReq : tToggleReq;
event eStatusResp: tStatusResp;