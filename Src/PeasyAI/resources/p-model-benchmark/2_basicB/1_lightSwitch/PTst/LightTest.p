machine TestSingleSwitch {
    start state Init {
        entry {
            SetupLightSystem(1);
        }
    }
}

machine TestMultipleSwitches {
    start state Init {
        entry {
            SetupLightSystem(choose(2) + 2); // 2-3 switches
        }
    }
}

fun SetupLightSystem(numSwitches: int) {
    var i: int;
    var light: Light;
    var initialLightState: bool;

    initialLightState = false;
    light = new Light();

    announce eSpec_LightToggleConsistency_Init, initialLightState;

    while(i < numSwitches) {
        new Switch((lightMachine = light, id=i));
        i = i+1;
    }
}

test tcSingleSwitch [main=TestSingleSwitch]: 
    assert LightToggleConsistency, EventualResponse in
    ({Switch, Light, TestSingleSwitch });

test tcMultipleSwitches [main=TestMultipleSwitches]:
    assert LightToggleConsistency, EventualResponse in
    ({ Switch, Light, TestMultipleSwitches });