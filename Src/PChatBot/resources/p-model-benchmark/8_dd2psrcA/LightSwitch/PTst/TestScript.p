test tcSingleSwitch [main=TestSingleSwitch]: 
    assert LightToggleConsistency, EventualResponse in
    ({Switch, Light, TestSingleSwitch });

test tcMultipleSwitches [main=TestMultipleSwitches]:
    assert LightToggleConsistency, EventualResponse in
    ({ Switch, Light, TestMultipleSwitches });