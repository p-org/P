//announce message
event Announce assume 2 : (machine, int, int);
//Initialise event to initialize the network (portconnectedto, myclock)
event Initialise assert 1 : (machine, machine);
//Initialize variables on PowerUp when machines start ((ParentGM))
event PowerUp assert 1 : (machine, int, int);
//Local transitions
event Local assert 1;
//State decision event sent periodically by parent clock to port machines
event StateDecisionEvent assume 1;

// all the below events are used for atomic state decision calculation
event Ack assert 2;
event ErBest assert 2 : (machine, (machine, int, int));
event UpdateParentGM assert 1 : (machine, int, int);
//Recommended state
event goMaster assert 1;
event goSlave assert 1;
event goPassive assert 1;

//Done state update
event doneStateChange assert 1;

//To join and break links in the System
event join assert 1 : machine;
event break assert 1 : machine;

//monitor the announce event
event mAnnounce assert 1 : int;
////////////////////////////////////////////////////////////
// THe topology under test is
// Initially 
// pt5_P(2)_pt1 -- pt2_P(3)_pt3 -- pt4_P(1)
//  |					_pt8_
// pt6_P(4)_pt7
// After break and join
// P(2) -- P(3) -break- P(1)
//  |		|
// P(4) --join
// If Rank = 1 it implies that the clock is a stratum 1 clock.
///////////////////////////////////////////////////////////

// main machine the got machine which creates the verification instance
main model GodMachine {
    fun seal() [invokescheduler = seal] {}
    fun unseal() [invokescheduler = unseal] {}

	//the clock nodes
	var P1 : machine;
	var P2 : machine;
	var P3 : machine;
	var	P4 : machine;
	//ports connecting the clocks together
	var pt1 : machine;
	var pt2 : machine;
	var	pt3 : machine;
	var	pt4 : machine;
	var pt5 : machine;
	var pt6 : machine;
	var pt7 : machine;
	var pt8 : machine;
	var temp : machine;
	//temp variable 
	var link:seq[machine];
	var scenario: seq[(int, machine, machine)]; // specifies the scenario (operation, clock, port)
	start state GodMachine_Init {
		
		entry {
			// create all the post machines
			seal();
			new RogueFrame();
			pt1 = new PortMachine();
			pt2 = new PortMachine();
			pt3 = new PortMachine();
			pt4 = new PortMachine();
			pt5 = new PortMachine();
			pt6 = new PortMachine();
			pt7 = new PortMachine();
			pt8 = new PortMachine();
			unseal();
			// create the ordinary clock machines
			link += (0,pt4);
			P1 = new Clock(link, 1);
			link -= (0);
			assert(sizeof(link) == 0);
			link += (0, pt6);
			P4 = new Clock(link, 4);
			link -= (0);
			assert(sizeof(link) == 0);
			//create the boundary clocks
			link += (0, pt5); link += (0, pt1);
			P2 = new Clock(link, 2);
			link -= (0); link -= (0);
			assert(sizeof(link) == 0);
			link += (0, pt2); link += (0,pt3);
			P3 = new Clock(link, 3);
			link -= (0); link -= (0);
			assert(sizeof(link) == 0);
			seal();
			
			//initialize all the ports appropriately with the connections and power them up
			send pt1, Initialise, (pt2, P2);
			send pt2, Initialise, (pt1, P3);
			send pt3, Initialise, (pt4, P3);
			send pt4, Initialise, (pt3, P1);
			send pt5, Initialise, (pt6, P2);
			send pt6, Initialise, (pt5, P4);
			send pt7, Initialise, (pt8, P4);
			send pt8, Initialise, (pt7, P3);
			unseal();
			
			//create the scenario testing machine
			scenario += (0, (0, P3, pt3));
			scenario += (0, (0, P1, pt4));
			scenario += (0, (1, P4, pt7));
			scenario += (0, (1, P3, pt8));
			new scenariotester(scenario);
			
			//delete the machine Gods job is done
			raise halt;
		}
	}
}

// Boundary clock 
// this machine manages multiple port state machines and also makes sure that the state-changes are atomic
// across the port machines

machine Clock {
    fun seal() [invokescheduler = seal] {}
    fun unseal() [invokescheduler = unseal] {}

	var Ports:seq[machine]; // Port machines in this clock
	var ParentGM : (machine, int, int); // pointer to the parent GM for this clock (machine machine, rank, stepsremoved)
	var lengthFromGM : int; // number of boundary clocks from the parent GM clock, it basically corresponds to the steps removed
	var D0 : int; // myRank
	var EBest : (machine, (machine, int, int)); // best message received in the current announce interval (received from, (GM, GM_rank, stepsRemoved))
	var ErBestSeq : seq[(machine, (machine, int, int))]; // ErBest from each port machine
	//temp variable
	var check : bool;
	var i:int;
	var countAck:int;
	
	start state Init {
		defer join, break;
		entry {
		
			seal();
			//initialize the EBest value to random
		    Ports = (payload as (seq[machine], int)).0;
		    D0 = (payload as (seq[machine], int)).1;
			ParentGM = (this, D0, 0);
			EBest = (null, (null, 100000, 0));
			countAck = 0;
			i = sizeof(Ports) - 1;
			while(i>=0)
			{
				send Ports[i], PowerUp, ParentGM;
				i = i - 1;
			}
			unseal();
			raise Local;
			
		}
			
		on Local goto PeriodicStateDecision;
	}
	
	fun ReceiveErBest() {
		//add to the sequence
		ErBestSeq += (0, payload as (machine, (machine, int, int)));
		assert(sizeof(ErBestSeq) <= sizeof(Ports));
		if(sizeof(ErBestSeq) == sizeof(Ports))
		{
			//calculate EBest and also clear ErBest
			i = sizeof(ErBestSeq) - 1;
			while(i >= 0)
			{
				if((EBest.1).1 > (ErBestSeq[i].1).1)
				{
					EBest = ErBestSeq[i];
				}
				i = i - 1;
			}
			raise(Local);
		}	
	}
	
	model fun IsPeriodicAnnounceTimeOut() : bool {
		if($)
			return true;
		else 
			return false;
	}
	
	fun BreakLink() {
		i = sizeof(Ports) - 1;
		while(i>=0)
		{
			if(Ports[i] == payload as machine)
			{
				Ports -= (i);
			}
			i = i - 1;
		}
	}
	
	fun JoinLink() {
		Ports += (0, payload as machine);
		send payload, PowerUp, ParentGM;
	}
	
	state PeriodicStateDecision {
		on join do JoinLink;
		on break do BreakLink;
		on ErBest do ReceiveErBest;
		
		entry {
			check = IsPeriodicAnnounceTimeOut();
			if(check)
			{
				i = sizeof(Ports) - 1;
				seal();
				while(i>=0)
				{
					//send state decision event to all the ports so that we can evaluate new state
					send Ports[i], StateDecisionEvent;
					i = i - 1;
				}
				unseal();
				//go to atomic transaction mode
				push WaitForErBest;
			}
		}
		on null goto PeriodicStateDecision;
	}
	
	state WaitForErBest {
		defer break,join;
		entry {
			assert(true);
		}
		on Local goto CalculateRecommendedState;
	}
	
	fun ReceiveAck() {
		countAck = countAck + 1;
		if(countAck == sizeof(Ports))
		{
			countAck = 0;
			i = sizeof(Ports) - 1;
			while(i >= 0)
			{
				send Ports[i], Ack;
				i = i - 1;
			}
			return;
		}
	}
	
	//This state implements the logic shown in figure 26
	state CalculateRecommendedState {
		on Ack do ReceiveAck;
		defer break, join;
		entry {
		
			i = sizeof(Ports) - 1;
			seal();
			//for each port calculate the recommended state
			while(i >= 0)
			{
				//check if I am the GM or my clock is better than all ErBest
				if(D0 == 1) // D0 is class stratum 1
				{
					if(D0 < (ErBestSeq[i].1).1) // D0 is better than EBest
					{
						//the parentGM point to current node
						ParentGM = (this, D0, 0);
						send ErBestSeq[i].0, goMaster; // BMC_Master M1
					}
					else
					{
						//no change in the parentGM
						send ErBestSeq[i].0, goPassive; // BMC_Passive P1
					}
				}
				else
				{
					if(D0 < (EBest.1).1)
					{
						//GM is the current node
						ParentGM = (this, D0, 0);
						send ErBestSeq[i].0, goMaster; // BMC_Master M2
					}
					else
					{
						//check on which port Ebest was received
						if(EBest.0 == ErBestSeq[i].0)
						{
							ParentGM = ErBestSeq[i].1;
							send ErBestSeq[i].0, goSlave; //BMC_Slave S1
						}
						else
						{
							if((EBest.1).1 < (ErBestSeq[i].1).1)
							{
								send ErBestSeq[i].0, goPassive; //BMC_Slave P2
							}
							else
							{
								ParentGM = EBest.1;
								send ErBestSeq[i].0, goMaster; //BMC_Master M3
							}
						}
					}
				}
				i = i - 1;
			}
			
			//clear the Erbest seq
			i = sizeof(ErBestSeq) - 1;
			while(i>=0)
			{
				ErBestSeq -= (0);
				i = i - 1;
			}
			
			//send all the ports their new ParentGM
			
			i = sizeof(Ports) - 1;
			while(i>=0)
			{
				send Ports[i], UpdateParentGM, (ParentGM.0, ParentGM.1, ParentGM.2 + 1); // increment the number of stepsremoved irrespective of OC / BC //// make 0 for OC. ??
				i = i - 1;
			}
			unseal();
			
		}
	}
}

// The port state machine

machine PortMachine {
	var ConnectedTo : machine;
	var MyClock : machine;
	var ErBestVar : (machine, int, int);
	var ParentGM : (machine, int, int);
	//temporary variables
	var check : bool;
	var numOfAnnounceIntervals: int;
	// 0 : master, 1: slave, 2 : passive
	var recState : int;
	
	start state PortInitState {
		ignore Announce;
		defer StateDecisionEvent, PowerUp;
		
		on Initialise goto ConnectionInitialized with
		{
				ConnectedTo = (payload as (machine, machine)).0;
				MyClock = (payload as (machine, machine)).1;
				ErBestVar = (this, 10000, 0);
				
		};
	}
	
	state ConnectionInitialized {
		ignore Announce;
		on PowerUp goto Initializing with
		{
			ParentGM = payload as (machine, int, int);
		};
	}
	model fun IsAnnounceReceiptTimeOut() : bool
	{
		if($)
			return true;
		else 
			return false;
	}
	
	model fun IsThreeAnnounceReceiptTimeOut() : bool
	{
		if($)
			return true;
		else 
			return false;
	}
	
	
	state Initializing {
		ignore Announce;
		defer StateDecisionEvent;
		entry {
			check = IsAnnounceReceiptTimeOut();
			if(check)
			{
				raise(Local);
			}
		}
		
		on null goto Initializing;
		on Local goto Listening;
	}
	
	fun HandleAnnounce() {
		if(ErBestVar.1 > payload.1)
		{
			ErBestVar = payload as (machine, int, int);
		}
	}
	state Listening {
		
		on Announce do HandleAnnounce;
		entry {
			check = IsThreeAnnounceReceiptTimeOut();
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
			check = IsAnnounceReceiptTimeOut();
			if(check)
			{
				monitor mAnnounce, ParentGM.2;
				send ConnectedTo, Announce, ParentGM;
			}
		}
		
		on null goto Master;
		on StateDecisionEvent goto DeferAll;
	}
	
	state DeferAll {
		defer Announce;
		entry {
			push SendErBestAndWaitForRecState;
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
	
	fun UpdateState() {
		if(trigger == goMaster)
			recState = 0;
		if(trigger == goSlave)
			recState = 1;
		if(trigger == goPassive)
			recState = 2;
	}
	state SendErBestAndWaitForRecState {
		entry {
			send MyClock, ErBest, (this, ErBestVar);
		}
		on goMaster do UpdateState;
		on goSlave do UpdateState;
		on goPassive do UpdateState;
		on UpdateParentGM goto WaitForAck with
		{
			ParentGM = payload as (machine, int, int);
			send MyClock, Ack;
		};
	}
	
	fun JustReturn() {
		return;
	}
	
	state WaitForAck {
		entry { }
		on Ack do JustReturn;
	}
	
	state Slave {
		on Announce do HandleAnnounce;
		entry {
			check = IsThreeAnnounceReceiptTimeOut();
			if(check)
				raise(goMaster);
		}
		on StateDecisionEvent goto DeferAll;
		on goMaster goto Master; // Doubt : What should be the value of ParentGM ??
		on null goto Slave;
	}
	
	state Passive {
		on Announce do HandleAnnounce;
		entry {
			check = IsThreeAnnounceReceiptTimeOut();
			if(check)
			{
				raise(goMaster);
			}
		}
		on goMaster goto Master;
		on null goto Passive;
		on StateDecisionEvent goto DeferAll;
	}
}
		
model scenariotester {
    fun seal() [invokescheduler = seal] {}
    fun unseal() [invokescheduler = unseal] {}

	var scenario: seq[(int, machine, machine)];
	var i:int;
	start state init {
		entry {
		      scenario = payload as seq[(int, machine, machine)];
			  raise(Local);
		}
		on Local goto breaklink;
	}
	
	state breaklink {
		entry {
			if($)
			{
				i = sizeof(scenario) -1;
				seal();
				while(i>=0)
				{
					if(scenario[i].0 == 0)
					{
						send scenario[i].1, break, scenario[i].2;
					}
					i = i - 1;
				}
				unseal();
				raise(Local);
			}
		}
		on null goto breaklink;
		on Local goto joinlink;
	}
	
	state joinlink {
		entry {
			if($)
			{
				seal();
				i = sizeof(scenario) -1;
				while(i>=0)
				{
					if(scenario[i].0 == 1)
					{
						send scenario[i].1, join, scenario[i].2;
					}
					i = i - 1;
				}
				unseal();
				raise(Local);
			}
		}
		on null goto joinlink;
		on Local goto Done;
	}
	
	state Done {
	}
}		
		
spec RogueFrame monitors mAnnounce {
	start state init {
		on mAnnounce goto recAnnouce;
	}
	
	state recAnnouce {
		entry {
			if((payload as int) > 100)
				assert(false);
		}
		on mAnnounce goto recAnnouce;
	}
}		
		
		
	
	
	
	
	

