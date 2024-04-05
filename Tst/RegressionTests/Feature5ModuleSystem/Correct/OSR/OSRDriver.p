machine OSRDriverMachine
sends eUpdateBarGraphStateUsingControlTransfer, eSetLedStateToStableUsingControlTransfer,
eSetLedStateToUnstableUsingControlTransfer, eStartDebounceTimer, eStopTimer;
{
	
	var TimerV: machine;
	var LEDV: machine;
	var SwitchV: machine;
	var check: bool;
	
	start state Driver_Init {
		defer eSwitchStatusChange;
		entry {
			TimerV = new TimerInterface(this to OSRDriverInterface);
			LEDV = new LEDInterface(this to OSRDriverInterface);
			SwitchV = new SwitchInterface(this to OSRDriverInterface);
			raise(eUnit);
		}
		
		on eUnit goto sDxDriver;
	}
	
	state sDxDriver {
		defer eSwitchStatusChange;
		ignore eD0Exit;
		
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
			raise(eUnit);
		}
		on eUnit goto sStoppingTimerDriver;
		on eTimerStopped goto sStoringSwitchAndCheckingIfStateChangedDriver;
	}
	
	state sStoppingTimerOnD0ExitDriver {
		defer eD0Exit, eSwitchStatusChange;
		ignore eD0Entry;
		
		entry {
			raise(eUnit);
		}
		
		on eTimerStopped goto sCompletingD0ExitDriver;
		on eUnit goto sStoppingTimerDriver;
		
	}
	
	state sStoppingTimerDriver {
		ignore eD0Entry;
		entry {
			send TimerV, eStopTimer;
		}
		
		on eStoppingSuccess goto sReturningTimerStoppedDriver;
		on eStoppingFailure goto sWaitingForTimerToFlushDriver;
		on eTimerFired goto sReturningTimerStoppedDriver;
	}
	
	state sWaitingForTimerToFlushDriver {
		defer eD0Exit, eSwitchStatusChange;
		ignore eD0Entry;
		
		entry {}
		
		on eTimerFired goto sReturningTimerStoppedDriver;
		
	}
	
	
	state sReturningTimerStoppedDriver {
		ignore eD0Entry;
		entry {
			raise(eTimerStopped);
		}
	}
}

	
	
		
