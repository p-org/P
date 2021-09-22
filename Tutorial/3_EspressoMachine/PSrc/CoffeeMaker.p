
/* Request from the controller to coffee maker */
// event: warmup request when the machine starts
event eWarmUpReq;
// event: grind beans request for a coffee
event eGrindBeansReq;
// event: start making cofee
event eStartEspressoReq;
// event start steamer
event eStartSteamerReq;
// event: stop steamer
event eStopSteamerReq;

/* Responses from the coffee maker to the controller */
// event: completed grinding beans
event eGrindBeansCompleted;
// event: completed making coffee
event eEspressoCompleted;
// event: warmed up the machine and read to make coffee
event eWarmUpCompleted;

/* Error messages from the coffee maker to control panel*/
// event: no water for coffee, refill water!
event eNoWaterError;
// event: no beans for coffee, refill beans!
event eNoBeansError;
// event: the heater to warm the machine is broken!
event eWarmerError;

/*****************************************************
EspressoCoffeeMaker machine receives requests from the control panel of the coffee machine and
based on whether it is in the correct state heater working, has beans and water, it responds back to
the controller.
*****************************************************/
machine EspressoCoffeeMaker
{
    // control panel of the coffee machine that sends inputs to the coffee maker
    var controller: CoffeeMakerControlPanel;

    start state WaitForRequests {
        entry (_controller: CoffeeMakerControlPanel) {
            controller = _controller;
        }

        on eWarmUpReq do {
            if(IsHeaterWorking())
                send controller, eWarmUpCompleted;
        }

        on eGrindBeansReq do {
            if (!HasBeans()) {
		        send controller, eNoBeansError;
	        } else {
		        send controller, eGrindBeansCompleted;
	        }
        }

        on eStartEspressoReq do {
           	if (!HasWater()) {
		        send controller, eNoWaterError;
	        } else {
		        send controller, eEspressoCompleted;
	        }
        }
        on eStartSteamerReq do {
            if (!HasWater()) {
		        send controller, eNoWaterError;
	        }
        }
        on eStopSteamerReq do { /* do nothing, steamer stopped */ }
    }

    // nondeterministic functions to trigger different behaviors
    fun HasBeans() : bool { return $; }
    fun HasWater() : bool { return $; }
    fun IsHeaterWorking(): bool { return $; }
}


