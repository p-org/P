/*
SlotTimerMachine : This machine is essentially the hardware timer machine that enqueues the event new slot.
NetworkManagerMachine : This machine broadcasts the schedule to all the machines and new schedule is created if new nodes join the System.
MoteMachine: This is the node machine.
GodMachine : This machine creates the network topology and non deterministically adds nodes into the system.
*/

/*
lets consider the static schedule :

N1 -- N2 -- N3
	   |	|
N4 ----------

let us consider N1 as the root node.
N1 - Rank 0
N2 - Rank 1
N3 - Rank 2
N4 - Rank 1
*/

/*
channel probability is predefined and can be modeled as a faulty channel
how do you machine the non det back off
*/

//events in the System

//this event is enqueued by the Timer machine. In real systems this will be replaced by the timer interrupt routine

event newSlot assert 1 : (bool, (machine,machine));
event endSlot;
event Local;
event TxDone;
//Local events inside mote that determines the operation to be performed
event Tx;
event Rx;
event Sleep;
event Data assert 4 : (machine,int);
event Ack assert 1 : (machine,int);
event Initialize assert 1 : (machine,seq[machine]);

machine Main {
	var N1:machine;var N2:machine;var N3:machine;var N4:machine;
	var templ:seq[machine];
	var slotT : machine;
	start state init {
		entry {
			N1 = new OpenWSN_Mote(0);
			N2 = new OpenWSN_Mote(1);
			N3 = new OpenWSN_Mote(2);
			N4 = new OpenWSN_Mote(1);
			//initalize the slot machine
			templ += (0, N1); templ += (0, N2); templ += (0, N3); templ += (0, N4);
			slotT = new SlotTimerMachine(templ);
			templ -= 0; templ -= 0; templ -= 0; templ -= 0; assert(sizeof(templ) == 0);
			//initialize the connection
			templ += (0, N2);
			send N1, Initialize, (slotT,templ);
			templ -= 0; assert(sizeof(templ) == 0);
			templ += (0, N1); templ += (0, N3); templ += (0, N4);
			send N2, Initialize, (slotT,templ);
			templ -= 0; templ -= 0; templ -= 0; assert(sizeof(templ) == 0);
			templ += (0, N2); templ += (0, N4);
			send N3, Initialize, (slotT,templ);
			templ -= 0; templ -= 0; assert(sizeof(templ) == 0);
			templ += (0, N2); templ += (0, N3);
			send N4, Initialize, (slotT,templ);
			templ -= 0; templ -= 0; assert(sizeof(templ) == 0);			
		}
	}

}
machine OpenWSN_Mote {
	//my ID
	var myRank:int;
	//temp variable
	var temp: int;
	var check : bool;
	//my neighbours determined static
	var myNeighbours: seq[machine];
	//preferred neighbour (time parent)
	var myTimeParent:(machine, int);
	//last synchronized
	var lastSynched: int;
	//current slot
	var currentSlot: (bool, (machine, machine)); //(isshared, machine)
	//slot timer
	var slotTimer:machine;
	//local
	var i:int;
	
	start state init_mote {
		defer newSlot;
		ignore Data;
		entry (payload: int){
			//init the connections
			myRank = payload;
			myTimeParent = (null as machine, 10000);
			lastSynched = 0;	
		
		}
		on Initialize goto WaitForNewSlot with (payload :(machine,seq[machine]))
		{
			slotTimer = payload.0;
			myNeighbours = payload.1;
		}
	}

	fun CheckOperationTobePerfomed(currentSlot :(bool, (machine,machine))) {
		if(myRank != 0)
			lastSynched = lastSynched + 1;
		temp = OperationTxorRxorSleep();
		if(temp == 0)
			raise Tx;
		if(temp == 1)
			raise Rx;
		if(temp == 2)
			raise Sleep;
	}
	
	fun OperationTxorRxorSleep() : int {
		if($)
			return 0; // Tx
		else if ($)
			return 1; // Rx
		else
			return 2; // Sleep
	}
	
	state WaitForNewSlot {
		ignore Data, Ack;
		on newSlot do (payload : (bool, (machine,machine))) { CheckOperationTobePerfomed(payload);}
		on Tx goto DataTransmissionMode;
		on Rx goto DataReceptionMode;
		on Sleep goto WaitForNewSlot with
		{
			send slotTimer, endSlot;
		}
	}
	
	fun TransmitData(target:machine) {
		if(target == null)
		{
			//choose non-det
			i = sizeof(myNeighbours) - 1;
			while(i>= 0)
			{
				if($)
				{
					send myNeighbours[i], Data, (this, myRank);
					return;
				}
				else
					i = i - 1;
			}
		}
		else
		{
			send target, Data, (this, myRank);
		}
	}
	
	fun CSMA_CA() : bool {
		if($)
		{
			return true;
		}
		else
			return false;
	}
	
	state DataTransmissionMode {
		entry {
		      if(!currentSlot.0)
			{
			if(currentSlot.1.0 == this)
				{
				TransmitData(currentSlot.1.1);
					raise TxDone;
				}
				else
				{
					raise Local;
				}
			}
			else
			{
				//this slot is shared
				check = CSMA_CA();
				if(check)
				{
					TransmitData(null as machine);
					raise TxDone;
				}
				else
				{
					raise Local;
				}
			}
			
		}
		on Local goto WaitForNewSlot with
		{
			send slotTimer, endSlot;
		}
		on TxDone goto WaitForAck;
	}
	
	state WaitForAck {
		ignore Data;
		entry {
		
		}
		on Ack goto WaitForNewSlot with (payload : (machine,int)) {
		{
			//update the timeparent
		if(myTimeParent.1 > payload.1)
				myTimeParent = payload;
				
		if(payload.0 == myTimeParent.0)
				lastSynched = 0; //Synched
				
			send slotTimer, endSlot;
		}}
		on null goto WaitForNewSlot with
		{
			send slotTimer, endSlot;
		}
	}
	
	state DataReceptionMode {
		entry {
			
		}
		on Data goto WaitForNewSlot with (payload : (machine,int))
		{
			//Update my preferred parent
		if(myTimeParent.1 > payload.1)
				myTimeParent = payload;
				
		if(payload.0 == myTimeParent.0)
				lastSynched = 0; //synched
			
		send payload.0, Ack, (this, myRank);
			
			send slotTimer, endSlot;
		}
	}
	
}

machine SlotTimerMachine {
	var AllMotes:seq[machine];
	var i: int;
	var counter: int;
	start state init{
		entry (payload : seq[machine]){
			counter = 0;
			AllMotes = payload;
			raise Local;
		}
		on Local goto SendNewSlot;
	}
	
	state SendNewSlot {
		entry {
			i = sizeof(AllMotes) - 1;
			while(i>=0)
			{
				send AllMotes[i], newSlot, (true, (null as machine, null as machine));
				i = i - 1;
			}
		}
		on endSlot do increaseCounter;
		on Local goto SendNewSlot;
	}
	
	fun increaseCounter() {
		counter = counter + 1;
		if(counter == sizeof(AllMotes))
		{
			counter = 0;
			raise Local;
		}
	}
}

