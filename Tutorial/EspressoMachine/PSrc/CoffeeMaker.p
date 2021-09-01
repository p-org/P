/*
The CoffeeMaker machine that receives requests from the ControlPanel and performs those operations
*/

event eWarmUpReq;
event eWarmUpCompleted;
event eGrindBeansReq;
event eStartEspressoReq;
event eStartSteamerReq;
event eStopSteamerReq;
event eNoWaterError;
event eNoBeansError;
event eGrindBeansCompleted;
event eEspressoCompleted;

machine EspressoCoffeeMaker
{
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
        on eStopSteamerReq do { /* steamer stopped */ }
    }

    fun HasBeans() : bool { return $; }
    fun HasWater() : bool { return $; }
    fun IsHeaterWorking(): bool { return $; }
}


