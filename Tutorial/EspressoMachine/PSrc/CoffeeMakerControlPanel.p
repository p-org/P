enum tCoffeeMakerState {
    NotWarmedUp,
    Ready,
    Error
}

event eEspressoButtonPressed;
event eSteamerButtonOff;
event eSteamerButtonOn;
event eOpenGroundsDoor;
event eCloseGroundsDoor;
event eResetCoffeeMaker;

/*
CoffeeMakerControlPanel that acts as the interface between the CoffeeMaker and User
*/
machine CoffeeMakerControlPanel
{
    var timer: Timer;
    var coffeeMaker: EspressoCoffeeMaker;
    var cofferMakerState: tCoffeeMakerState;

    start state Init {
        entry {
            cofferMakerState = NotWarmedUp;
            coffeeMaker = new EspressoCoffeeMaker(this);
            timer = CreateTimer(this);
            goto WarmUpCoffeeMaker;
        }
    }
    
    state WarmUpCoffeeMaker {
        entry {
            StartTimer(timer);
            BeginHeatingCoffeeMaker();
        }

        on eTimeOut goto EncounteredError with {
            print "Failed to heat-up the CoffeeMaker in time, Please reset the machine!";
        }
        on eWarmUpCompleted goto CoffeeMakerReady with {
            CancelTimer(timer);
        }

        defer eOpenGroundsDoor, eCloseGroundsDoor; // grounds door is open will handle it later
        ignore eEspressoButtonPressed, eSteamerButtonOff, eSteamerButtonOn; // ignore these inputs from users until the maker has warmed up.
    }




    state CoffeeMakerReady {
        entry {
            cofferMakerState = Ready;
        }
        on eOpenGroundsDoor goto CoffeeMakerDoorOpened;
        on eEspressoButtonPressed goto CoffeeMakerRunGrind;
        on eSteamerButtonOn goto CoffeeMakerRunSteam;
        ignore eSteamerButtonOff, eCloseGroundsDoor;
    }

    state CoffeeMakerRunGrind {
        entry {
            GrindBeans();
        }
        on eNoBeansError goto EncounteredError with {
            print "No beans to grind! Please refill beans and reset the machine!";
        }
        on eGrindBeansCompleted goto CoffeeMakerRunEspresso;

        defer eOpenGroundsDoor, eCloseGroundsDoor;
        // Can't make steam while we are making espresso
        ignore eSteamerButtonOn, eSteamerButtonOff;
    }

    state CoffeeMakerRunEspresso {
        entry {
            StartEspresso();
        }
        on eEspressoCompleted goto CoffeeMakerReady;
        defer eOpenGroundsDoor, eCloseGroundsDoor;
        // Can't make steam while we are making espresso
        ignore eSteamerButtonOn, eSteamerButtonOff;
    }

    state CoffeeMakerRunSteam {
        entry {
            StartSteamer();
        }
        on eSteamerButtonOff  goto CoffeeMakerReady with {
            StopSteamer();
        }
        defer eOpenGroundsDoor, eCloseGroundsDoor;
        // can't make espresso or steam while we are making steam
        ignore eEspressoButtonPressed, eSteamerButtonOn;
    }

    state CoffeeMakerDoorOpened {
        on eCloseGroundsDoor do {
            if(cofferMakerState == NotWarmedUp)
                goto WarmUpCoffeeMaker;
            else
                goto CoffeeMakerReady;
        }
        ignore eEspressoButtonPressed, eSteamerButtonOn, eSteamerButtonOff; // grounds door is open cannot handle these requests just ignore them
    }
    
    state EncounteredError {
        entry {
            cofferMakerState = Error;
        }
        on eResetCoffeeMaker goto WarmUpCoffeeMaker;
        defer eOpenGroundsDoor, eCloseGroundsDoor; // door opened and closed, will handle these signals later.
        ignore eEspressoButtonPressed, eSteamerButtonOn, eSteamerButtonOff; // error, ignore these requests.
    }

    fun BeginHeatingCoffeeMaker() {
        // send an event to maker to start warming
        send coffeeMaker, eWarmUpReq;
    }

    fun StartSteamer() {
        // send an event to maker to start steaming
        send coffeeMaker, eStartSteamerReq;
    }

    fun StopSteamer() {
        // send an event to maker to stop steaming
        send coffeeMaker, eStopSteamerReq;
    }

    fun GrindBeans() {
        // send an event to maker to grind beans
        send coffeeMaker, eGrindBeansReq;
    }

    fun StartEspresso() {
        // send an event to maker to start espresso
        send coffeeMaker, eStartEspressoReq;
    }
}