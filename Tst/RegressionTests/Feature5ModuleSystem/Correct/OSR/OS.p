machine SwitchMachine
sends eSwitchStatusChange;
{
	var Driver: OSRDriverInterface;
    start state _Init {
			entry (payload: machine) { Driver = payload as OSRDriverInterface; raise(eUnit); }
	        on eUnit goto Switch_Init;
	    }

    state Switch_Init {
      entry { raise(eUnit);}
      on eUnit goto ChangeSwitchStatus;
    }
	
	

  state ChangeSwitchStatus {
		entry {
	     send Driver, eSwitchStatusChange;
	     raise (eUnit);		 	
		}
        on eUnit goto ChangeSwitchStatus;
    }
}

machine LEDMachine
sends eTransferSuccess, eTransferFailure;
{
	var Driver: OSRDriverInterface;
  start state _Init {
		entry (payload: machine) { Driver = payload as OSRDriverInterface; raise(eUnit); }
      on eUnit goto LED_Init;
  }

	state LED_Init {
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

machine TimerMachine
sends eTimerFired, eStoppingFailure, eStoppingSuccess;
{
	var Driver : OSRDriverInterface;
	
  start state _Init {
		entry (payload: machine) { Driver = payload as OSRDriverInterface; raise(eUnit); }
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
		
