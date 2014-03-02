//announce message
event Announce: (mid, int) assume 3;
//Initialize variables on PowerUp when machines start
event PowerUp: (mid, mid, int) assert 1;
//Local transitions
event Local assert 1;
//State decision event sent periodically by parent clock to port machines
event StateDecisionEvent assume 1;

// all the below events are used for atomic state decision calculation
event Ack assert 2;
event ErBest : (mid, int) assert 2;

//Recommended state
event goMaster assert 1;
event goSlave assert 1;
event goPassive assert 1;

//Done state update
event doneStateChange assert 1;

// main machine the got machine which creates the verification instance
main ghost machine GodMachine {

	var OC1 : id;
	var OC2 : id;
	var BC1 : id;
	var	BC2 : id;
	var pt1 : id;
	var pt2 : id;
	var	pt3 : id;
	var	pt4 : id;
	var pt5 : id;
	var pt6 : id;
	
	start state GodMachine_Init {
		
		entry {
			pt1 = new PortMachine();
			pt2 = new PortMachine();
			pt3 = new PortMachine();
			pt4 = new PortMachine();
			pt5 = new PortMachine();
			pt6 = new PortMachine();
			OC1 = new OrdinaryClock((pt1, null, 1));
			OC2 = new OrdinaryClock((pt6, null, 2));
			BC1 = new BoundaryClock((pt2,);
			BC2 = new BoundaryClock(P = (pt4, pt5) , D0 = (4, null, 0));
			BC3 = new BoundaryClock(P = (pt6, pt7) , D0 = (5, null, 0));
			
			send(pt1, PowerUp, (pt2, MasterOC, 1));
			send(pt2, PowerUp, (pt1, BC1, 2));
			send(pt3, PowerUp, (pt4, BC1, 2));
			send(pt4, PowerUp, (pt3, BC2, 3));
			send(pt5, PowerUp, (pt6, BC2, 3));
			send(pt6, PowerUp, (pt5, BC3, 4));
			send(pt7, PowerUp, (pt8, BC3, 4));
			send(pt8, PowerUp, (pt7, SlaveOC, 5));
			delete;
		}
	}
}

// Boundary clock 
// this machine manages multiple state machines and also makes sure that the state-changes are atomic
//

machine BoundaryClock {

	var P:(mid, mid);
	var Counter : int;
	var ParentGM : mid; // pointer to the parent GM
	var lengthFromGM : int; // number of boundary clocks from the GM
	var D0 : (int, mid); // 0: Rank, 1: gmID 
	var EBest : (mid, int);
	var ErBestV : ((mid, int), (mid, int));
	var check : bool;
	
	start state BSM_Init {
		entry {
			EBest = (this, 100);
			Counter = 0;
			raise(Local);
		}
			
		on Local goto PeriodicStateDecision;
	}
	
	action ReceiveErBest {
		
		if(EBest[1] > payload[1])
		{
			EBest = payload;
		}
		if(Counter == 0)
			ErBestV[0] = payload;
		else
			ErBestV[1] = payload;
		Counter = Counter + 1;
		if(Counter == 2)
		{
			Counter = 0;
			raise(Local);
		}
	}
	
	foreign fun IsPeriodicTimeOut() : bool {
		if(*)
			return true;
		else 
			return false;
	}
	
	state PeriodicStateDecision {
	
		on ErBest do ReceiveErBest;
		
		entry {
			check = IsPeriodicTimeOut();
			if(check)
			{
				send(P[0], StateDecisionEvent);
				send(P[1], StateDecisionEvent);
				call(WaitForErBest);
			}
		}
		
		on default goto PeriodicStateDecision;
	}
	
	state WaitForErBest {
		 entry {}
		 on Local goto CalculateRecommendedState;
	}
	
	action ReceiveAck {
	
		Counter = Counter + 1;
		if(Counter == 2)
		{
			Counter = 0;
			send(P[0], Ack);
			send(P[1], Ack);
			return;
		}
	}
	
	foreign fun IsBetter(mid machine) : bool {
	// Calculate if the machine is better by path or distance in the explorer
	// my lengthToGM > machine.lengthToGM + 1
	}
	
	foreign fun UpdateLocalData(mid machine) {
	// Update the Parent GM and the length to GM in the explorer
	// use the ebest value already updated
	// (1) set lengthToParent = ebest.lengthtoparent
	// (2) set parentGm = ebest.ParentGM.
	}
	state CalculateRecommendedState {
		on Ack do ReceiveAck;
		entry {
			//check if I am the GM or my clock is better than all ErBest
			UpdateLocalData(Erbest[0]);
			if((D0[0] == 1) || (D0[0] < EBest[1]))
			{
				send(P[0], goMaster);
				send(P[1], goMaster);
			}
			else if ((D0[0] == 1))
			{
				if(IsBetter(ErBest[0][0]))
				{
					send(P[0], goMaster);
				}
				else
				{
					send(P[0], goPassive);
				}
				if(IsBetter(ErBest[1][0]))
				{
					send(P[1], goMaster);
				}
				else
				{
					send(P[1], goPassive);
				}
				
			}
			else if(EBest[1] == ErBestV[0][1])
			{
				send(ErBestV[0][0], goSlave);
			}
			else if(EBest[1] == ErBestV[1][1])
			{
				send(ErBestV[1][0], goSlave);
			}
			else if(EBest[1] > ErBestV[0][1])
			{
				send(ErBestV[0][0], goMaster);
			}
			else if(EBest[1] > ErBestV[1][1])
			{
				send(ErBestV[1][0], goMaster);
			}
			else
			{
				if(!IsBetter(ErBest[0][0]))
				{
					send(P[0], goPassive);
				}
				if(!IsBetter(ErBest[1][0]))
				{
					send(P[1], goPassive);
				}
			}
		}
	}
}

// Ordinary Clock State Machine

machine OrdinaryClock {
	var P:mid;
	var D0 : (int, mid, int); // 0: priority, 1: gmID , 2 : gmPriority
	var EBest : (mid, int);
	var check : bool;
	var ParentGM : mid; // pointer to the parent GM
	var lengthFromGM : int; // number of boundary clocks from the GM
	
	
	start state OSM_Init {
		entry { 
			EBest = (this, 100);
			raise(Local);
		}
		on Local goto PeriodicStateDecision;
	}
	
	action ReceiveErBest {
		EBest = payload;
	}
	
	foreign fun IsPeriodicTimeOut() : bool {
		if(*)
			return true;
		else 
			return false;
	}
	
	
	state PeriodicStateDecision {
		on ErBest do ReceiveErBest;
		entry {
			check =  IsPeriodicTimeOut();
			if(check)
			{
				send(P, StateDecisionEvent);
				call(WaitForErBest);
			}
		}
		
		on default goto PeriodicStateDecision;
	}
	
	state WaitForErBest {
		 entry {}
		 on Local goto CalculateRecommendedState;
	}
	
	action ReceiveAck {
		send(P, Ack);
		return;
	}
	
	state CalculateRecommendedState {
		on Ack do ReceiveAck;
		entry {
			if((D0[0] == 1) || (D0[0] < EBest[1]))
			{
				send(P, goMaster);
			}
			else
			{
				send(P, goSlave);
			}
		}
	}
	
}

// The port state machine

machine PortMachine {
	var receivedMessage: bool;
	var ConnectedTo : mid;
	var ParentClock : mid;
	var ErBestV : (mid, int);
	var check : bool;
	// 0 : master, 1: slave, 2 : passive
	var recState : int;
	
	start state PortInitState {
		ignore Announce;
		defer StateDecisionEvent;
		entry { }
		exit {
			if(trigger == PowerUp)
			{
				ConnectedTo = (((mid, mid, int))payload)[0];
				ParentClock = (((mid, mid, int))payload)[1];
				ErBestV[0] = this;
				ErBestV[1] = (((mid, mid, int))payload)[2];
			}
		}
		on PowerUp goto Initializing;
	}
	
	foreign fun IsMasterTimeOut() : bool {
	// return true if announce time interval has reached
	}
	
	foreign fun IsSlaveTimeOut() : bool {
	// return true if receivedMessage is false after 3 announce intervals
	}
	
	foreign fun IsPassiveTimeOut() : bool {
	// return true if receivedMessage is false after 3 announce intervals
	}
	
	foreign fun UpdateLocalData(mid machine) {
	// Update the Parent GM and the length to GM
	//algorithm 
	//(1) if the sender is boundary clock lengthToGM = machine.lengthToGM + 1;
	//(2) parentGM = machine.parentGM.
	}
	
	state Initializing {
		ignore Announce;
		defer StateDecisionEvent;
		entry {
			check = IsTimeOut();
			if(check)
			{
				raise(Local);
			}
		}
		
		on default goto Initializing;
		on Local goto Listening;
	}
	
	action HandleAnnounce {
		if(ErBestV[1] > payload[1])
		{
			UpdateLocalData(payload);
			ErBestV[1] = (((mid, int)) payload)[1];
		}
	}
	state Listening {
		
		on Announce do HandleAnnounce;
		entry {
			check = IsTimeOut();
			if(check)
			{
				raise(goMaster);
			}
		}
		on StateDecisionEvent goto DeferAll;
		on goMaster goto Master;
	}

	state Master {
		on Announce do HandleAnnounce;
		entry {
			check = IsMasterTimeOut();
			if(check)
				send(ConnectedTo, Announce, ErBestV);
		}
		
		on default goto Master;
		on goSlave goto Slave;
		on StateDecisionEvent goto DeferAll;
	}
	
	state DeferAll {
		defer Announce;
		entry {
			call(SendErBestAndWaitForRecState);
			if(recState == 0)
				raise(goMaster);
			if(recState == 1)
				raise(goSlave);
			if(recState == 2)
				raise(goPassive);
		}
		on goMaster goto Master;
		on goSlave goto Slave;
		on goPassive goto Passive;
	}
	
	state SendErBestAndWaitForRecState {
		entry {
			send(ParentClock, ErBest, ErBestV);
		}
		on goMaster goto WaitForAck;
		on goSlave goto WaitForAck;
		exit {
			if(trigger == goMaster)
				recState = 0;
			if(trigger == goSlave)
				recState = 1;
			if(trigger == goPassive)
				recState = 2;
			send(ParentClock, Ack);
		}
	}
	
	action JustReturn {
		return;
	}
	
	state WaitForAck {
		entry { }
		on Ack do JustReturn;
	}
	
	state Slave {
		on Announce do HandleAnnounce;
		entry {
			check = IsSlaveTimeOut();
			if(check)
				raise(goMaster);
		}
		
		on goMaster goto Master;
		on default goto Slave;
	}
	
	state Passive {
		on Announce do HandleAnnouce;
		entry {
			check = IsPassiveTimeout();
			if(check)
			{
				raise(goMaster);
			}
		}
		on goMaster goto Master;
		on default goto Passive;
	}
}
		
		
		
		
		
		
	
	
	
	
	

