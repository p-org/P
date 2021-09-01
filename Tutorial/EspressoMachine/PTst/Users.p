/*
A SaneUser who knows how to use the CoffeeMaker
*/
machine SaneUser {
    var coffeeMakerPanel: CoffeeMakerControlPanel;
    var cups: int;
    start state Init {
        entry (coffeeMaker: CoffeeMakerControlPanel) {
            coffeeMakerPanel =  coffeeMaker;
            // want to make 2 cups of espresso
            cups = 2;
            //goto LetsMakeCoffee;
        }
    }

    state LetsMakeCoffee {
        entry {
            while (cups > 0)
            {
                // press Espresso button
                PerformOperationOnCoffeeMaker(coffeeMakerPanel, CM_PressEspressoButton);

                // check the status of the machine
                receive {
                    case eEspressoCompleted: {
                        // lets make the next coffee
                        cups = cups - 1;
                    }
                    case eNoBeansError, eNoWaterError, eWarmerError: {

                        // lets fill the beans or water and reset the machine
                        // and go back to making espresso
                        PerformOperationOnCoffeeMaker(null as CoffeeMakerControlPanel, CM_PressResetButton);
                    }
                }
            }

            // I am a good user and would clear the coffee grounds.
            PerformOperationOnCoffeeMaker(coffeeMakerPanel, CM_ClearGrounds);
        }
    }
}

enum tCoffeeMakerOperations {
    CM_PressEspressoButton,
    CM_PressSteamerButton,
    CM_PressResetButton,
    CM_ClearGrounds
}
/*
A crazy user who gets excited by looking at a coffee machine and starts stress testing the machine
by pressing all sorts of random button and opening/closing doors
*/
// todo: We do not support global constants currently, they can be encoded using global functions.

machine CrazyUser {
    var coffeeMakerPanel: CoffeeMakerControlPanel;
    var numOperations: int;
    start state StartPressingButtons {
        entry (config: (coffeeMaker: CoffeeMakerControlPanel, nOps: int)) {
            var pickedOps: tCoffeeMakerOperations;

            numOperations = config.nOps;
            coffeeMakerPanel = config.coffeeMaker;

            while(numOperations > 0)
            {
                pickedOps = PickRandomOperationToPerform();
                PerformOperationOnCoffeeMaker(coffeeMakerPanel, pickedOps);
                numOperations = numOperations - 1;
            }
        }

        // I will ignore all the responses from the coffee maker
        ignore eNoWaterError, eNoBeansError, eGrindBeansCompleted, eEspressoCompleted;
    }

    fun PickRandomOperationToPerform() : tCoffeeMakerOperations {
        var op_i: int;
        op_i =  choose(3);
        if(op_i == 0)
            return CM_PressEspressoButton;
        else if(op_i == 1)
            return CM_PressSteamerButton;
        else if(op_i == 2)
            return CM_PressResetButton;
        else
            return CM_ClearGrounds;
    }
}


/* Function to perform an operation on the CoffeeMaker */
fun PerformOperationOnCoffeeMaker(coffeeMakerCP: CoffeeMakerControlPanel, CM_Ops: tCoffeeMakerOperations)
{
    if(CM_Ops == CM_PressEspressoButton) {
        send coffeeMakerCP, eEspressoButtonPressed;
    }
    else if(CM_Ops == CM_PressSteamerButton) {
        send coffeeMakerCP, eSteamerButtonOn;
        // wait for some time and then release the button
        send coffeeMakerCP, eSteamerButtonOff;
    }
    else if(CM_Ops == CM_ClearGrounds)
    {
        send coffeeMakerCP, eOpenGroundsDoor;
        // empty ground and close the door
        send coffeeMakerCP, eCloseGroundsDoor;
    }
    else if(CM_Ops == CM_PressResetButton)
    {
        send coffeeMakerCP, eResetCoffeeMaker;
    }
}

module Users = { SaneUser, CrazyUser };