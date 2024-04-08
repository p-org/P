//zing takes about 5 min to come up with the "pass" result
event eD0Entry assume 1;
event eD0Exit assume 1;
event eTimerFired assert 1;
event eSwitchStatusChange assume 1;
event eTransferSuccess assume 1;
event eTransferFailure assume 1;
event eStopTimer assume 1;
event eUpdateBarGraphStateUsingControlTransfer assume 1;
event eSetLedStateToUnstableUsingControlTransfer assume 1;
event eStartDebounceTimer assume 1;
event eSetLedStateToStableUsingControlTransfer assume 1;
event eStoppingSuccess assert 1;
event eStoppingFailure assert 1;
event eOperationSuccess assert 1;
event eOperationFailure assert 1;
event eTimerStopped assert 1;
event eYes assert 1;
event eNo assert 1;
event eUnit assert 1;



machine Main {
    var Driver: machine;
    var count: int;

	start state User_Init {
		entry{
			Driver = new OSRDriver();
			raise(eUnit);
		}
		on eUnit goto S0;
	}
	
	state S0 {
		entry{
			send Driver, eD0Entry;
			raise(eUnit);
		}
		on eUnit do {
                    if(count < 1) { count = count + 1; goto S1; }
                }
	}
	
	state S1 {
		entry {
			send Driver, eD0Exit;
			raise(eUnit);
		}
		on eUnit goto S0;
	}

}

machine Switch {
	var Driver: machine;
        var count: int;
    start state _Init {
	entry (payload: machine) { Driver = payload; raise(eUnit); }
        on eUnit goto Switch_Init;
    }

    state Switch_Init {
        entry { raise(eUnit);}
        on eUnit goto ChangeSwitchStatus;
    }
	
	

    state ChangeSwitchStatus {
	entry {
	     send Driver, eSwitchStatusChange;
             count = count + 1;
             if (count < 3) {
	         raise (eUnit);		 	
             }
	}
        on eUnit goto ChangeSwitchStatus;
    }
}

machine LED {
	var Driver: machine;
	
    start state _Init {
	entry (payload: machine) { Driver = payload; raise(eUnit); }
        on eUnit goto LED_Init;
    }

	state LED_Init {
		entry { }
		
		on eUpdateBarGraphStateUsingControlTransfer goto ProcessUpdateLED;
		on eSetLedStateToUnstableUsingControlTransfer goto UnstableLED;
		on eSetLedStateToStableUsingControlTransfer goto StableLED;
	}
	
	state ProcessUpdateLED {
		entry {
			if($)
			{
				send Driver, eTransferSuccess;
			}
			else
				send Driver, eTransferFailure;
			raise(eUnit);
		}
		
		on eUnit goto LED_Init;
	}
	
	state UnstableLED {
		entry {
			send Driver, eTransferSuccess;
		}
		
		on eSetLedStateToStableUsingControlTransfer goto LED_Init;
		on eUpdateBarGraphStateUsingControlTransfer goto ProcessUpdateLED;
		
	}
	
	state StableLED {
		entry {
			send Driver, eTransferSuccess;
			raise(eUnit);
		}
		
		on eUnit goto LED_Init;
	}
}

machine Timer {
	var Driver : machine;
	
    start state _Init {
	entry (payload: machine) { Driver = payload; raise(eUnit); }
        on eUnit goto Timer_Init;
    }

	state Timer_Init {
		ignore eStopTimer;
		entry { }
		on eStartDebounceTimer goto TimerStarted;
	}
	
	state TimerStarted {
	
		defer eStartDebounceTimer;
		entry {
			if($)
				raise(eUnit);
		}
		
		on eUnit goto SendTimerFired;
		on eStopTimer goto ConsmachineeringStoppingTimer;
	}
	
	state SendTimerFired {
		defer eStartDebounceTimer;
		entry {
			send Driver, eTimerFired;
			raise(eUnit);
		}
		
		on eUnit goto Timer_Init;
	}

	state ConsmachineeringStoppingTimer {
		defer eStartDebounceTimer;
		entry {
			if($)
			{
				send Driver, eStoppingFailure;
				send Driver, eTimerFired;
			}
			else
			{
				send Driver, eStoppingSuccess;
			}
			raise(eUnit);
		}
	
	
		on eUnit goto Timer_Init;
	}
}
		
machine OSRDriver {
	
	var TimerV: machine;
	var LEDV: machine;
	var SwitchV: machine;
	var check: bool;
	
	start state Driver_Init {
		defer eSwitchStatusChange;
		entry {
			TimerV = new Timer(this);
			LEDV = new LED(this);
			SwitchV = new Switch(this);
			raise(eUnit);
		}
		
		on eUnit goto sDxDriver;
	}
	
	state sDxDriver {
		defer eSwitchStatusChange;
		ignore eD0Exit;
		
		entry {}
		
		on eD0Entry goto sCompleteD0EntryDriver;
	}
	
	state sCompleteD0EntryDriver {
		defer eSwitchStatusChange;
		entry {
			CompleteDStateTransition();
			raise(eOperationSuccess);
		}
		
		on eOperationSuccess goto sWaitingForSwitchStatusChangeDriver;
	}
	
	 fun CompleteDStateTransition() { }
	
	state sWaitingForSwitchStatusChangeDriver {
		ignore eD0Entry;
		entry {}
		on eD0Exit goto sCompletingD0ExitDriver;
		on eSwitchStatusChange goto sStoringSwitchAndCheckingIfStateChangedDriver;
		
	}
	
	state sCompletingD0ExitDriver {
	
		entry {
			CompleteDStateTransition();
			raise(eOperationSuccess);
		}
		
		on eOperationSuccess goto sDxDriver;
	}
	
	 fun StoreSwitchAndEnableSwitchStatusChange() { }
	
	 fun CheckIfSwitchStatusChanged() : bool {
		if($)
			return true;
		else
			return false;
	}
	
	 fun UpdateBarGraphStateUsingControlTransfer () {
		send LEDV, eUpdateBarGraphStateUsingControlTransfer;
	}
	
	 fun SetLedStateToStableUsingControlTransfer() {
		send LEDV, eSetLedStateToStableUsingControlTransfer;
	}
	
	 fun SetLedStateToUnstableUsingControlTransfer() {
		send LEDV, eSetLedStateToUnstableUsingControlTransfer;
	}
	
	 fun StartDebounceTimer() {
		send TimerV, eStartDebounceTimer;
	}
	
	state sStoringSwitchAndCheckingIfStateChangedDriver {
		ignore eD0Entry;
		entry {
			StoreSwitchAndEnableSwitchStatusChange();
			check = CheckIfSwitchStatusChanged();
			if(check)
				raise(eYes);
			else
				raise(eNo);
		}
		
		on eYes goto sUpdatingBarGraphStateDriver;
		on eNo goto sWaitingForTimerDriver;
	}
	
	state sUpdatingBarGraphStateDriver {
		ignore eD0Entry;
		defer eD0Exit, eSwitchStatusChange;
		entry {
			UpdateBarGraphStateUsingControlTransfer();
		}
		
		on eTransferSuccess goto sUpdatingLedStateToUnstableDriver;
		on eTransferFailure goto sUpdatingLedStateToUnstableDriver;
		
	}
	
	state sUpdatingLedStateToUnstableDriver {
		defer eD0Exit, eSwitchStatusChange;
		ignore eD0Entry;
		
		entry {
			SetLedStateToUnstableUsingControlTransfer();
		}
		
		on eTransferSuccess goto sWaitingForTimerDriver;
	}
	
	state sWaitingForTimerDriver {
		ignore eD0Entry;
		entry {
			StartDebounceTimer();
		}
		
		on eTimerFired goto sUpdatingLedStateToStableDriver;
		on eSwitchStatusChange goto sStoppingTimerOnStatusChangeDriver;
		on eD0Exit goto sStoppingTimerOnD0ExitDriver;
		
	}
		
	state sUpdatingLedStateToStableDriver {
		ignore eD0Entry;
		defer eD0Exit, eSwitchStatusChange;
		
		entry {
			SetLedStateToStableUsingControlTransfer();
		}
		
		on eTransferSuccess goto sWaitingForSwitchStatusChangeDriver;
	}
	
	state sStoppingTimerOnStatusChangeDriver {
		ignore eD0Entry;
		defer eD0Exit, eSwitchStatusChange;
		
		entry {
			goto sStoppingTimerDriver;
		}
		on eTimerStopped goto sStoringSwitchAndCheckingIfStateChangedDriver;
	}
	
	state sStoppingTimerOnD0ExitDriver {
		defer eD0Exit, eSwitchStatusChange;
		ignore eD0Entry;
		
		entry {
			goto sStoppingTimerDriver;
		}
		
		on eTimerStopped goto sCompletingD0ExitDriver;
		
	}
	
	state sStoppingTimerDriver {
		ignore eD0Entry;
		defer eSwitchStatusChange;
		defer eD0Exit;
		entry {
			send TimerV, eStopTimer;
		}
		
		on eStoppingSuccess goto sCompletingD0ExitDriver;
		on eStoppingFailure goto sWaitingForTimerToFlushDriver;
		on eTimerFired goto sCompletingD0ExitDriver;
	}
	
	state sWaitingForTimerToFlushDriver {
		defer eD0Exit, eSwitchStatusChange;
		ignore eD0Entry;
		
		entry {}
		
		on eTimerFired goto sCompletingD0ExitDriver;
		
	}
}

	
	
		
