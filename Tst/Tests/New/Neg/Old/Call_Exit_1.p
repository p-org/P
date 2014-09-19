event BookCab assert 2;
event BookFlight assert 2;
event FlightBooked assert 2;
event TryAgain assert 2;
event CabBooked assert 2; 
event Thanks assert 2; 
event ReachedAirport assert 2; 
event MissedFlight assert 2; 
event TookFlight assert 2; 
event Unit assert 2;

main machine Employee {
    var TravelAgentmachine: model;
    var CityCabmachine: model;
    var Check: bool;
    var RemoteCheckIn: bool;

    start state Init {
        entry {
            TravelAgentmachine = new TravelAgent(this); 
            CityCabmachine = new CityCab(this);
            RemoteCheckIn = false;
            raise Unit;
        }
            on Unit goto BangaloreOffice;
        }

    state BangaloreOffice {
        entry {
           push SBookFlight;
        }
        exit {
            if (trigger == FlightBooked) send TravelAgentmachine, Thanks;
        }
        on TryAgain goto BangaloreOffice;
        on FlightBooked goto SBookCab;
    }

    state SBookFlight { 
        entry {
            send TravelAgentmachine, BookFlight; 
            pop; 
        }
    }

    state SBookCab { 
        entry { 
            send CityCabmachine, BookCab; 
        }
        exit { 
            assert(RemoteCheckIn == false); 
            if (trigger != null) send CityCabmachine, Thanks;
        }

        on default goto TakeBus with { RemoteCheckIn = true; };
        on CabBooked goto TakeCab with { RemoteCheckIn = true; };
    }
	
    state TakeCab {
        entry { 
            raise ReachedAirport; 
        }
        on ReachedAirport goto AtAirport;
    }

    state TakeBus {
        entry { 
            raise ReachedAirport; 
        }
        on ReachedAirport goto AtAirport;
    }

    state AtAirport{
        entry {
            assert(RemoteCheckIn == true);
            Check = AmILucky();
            if (Check) {
                raise TookFlight;
            } else {
                raise MissedFlight;
            }
        }

        exit {
            RemoteCheckIn = false;
        }

        on TookFlight goto ReachedRedmond;
        on MissedFlight goto BangaloreOffice;
    }

    state ReachedRedmond {
        entry { assert(false); }
    }

    model fun AmILucky():bool { 
        if ($) 
            return true;
        else
            return false;
    }
}

model TravelAgent {
    var Employeemachine: machine;

    start state _Init {
      entry {
	    Employeemachine = payload as machine;
	    raise Unit;
      }
      on Unit goto Init;
    }

    state Init { 
        on BookFlight goto SBookFlight;
    }

    state SBookFlight {
        entry { 
            if ($) { send Employeemachine, TryAgain; raise Unit; } else { send Employeemachine, FlightBooked; } 
        }
        on Unit goto Init;
        on Thanks goto Init;
    }
}

model CityCab {
    var Employeemachine: machine;

    start state _Init {
      entry {
	    Employeemachine = payload as machine;
	    raise Unit;
      }
      on Unit goto Init;
    }

    state Init { 
        on BookCab goto SBookCab;
    }

    state SBookCab {
        entry { 
            if ($) { raise Unit;} else { send Employeemachine, CabBooked; }
        } 
        on Unit goto Init;
        on Thanks goto Init;
    }
}
