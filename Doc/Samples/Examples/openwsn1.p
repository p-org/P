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
how do you model the non det back off
*/

//events in the System

//this event is enqueued by the Timer machine. In real systems this will be replaced by the timer interrupt routine 

event newSlot:(bool, (id,id)) assert 1;
event endSlot;
event Local;
event TxDone;
//Local events inside mote that determines the operation to be performed
event Tx;
event Rx;
event Sleep;
event Data:(id,int) assert 4;
event Ack:(id,int) assert 1;
event Initialize:(mid,seq[id]) assert 1;

main model machine GodMachine {
	var N1:id;var N2:id;var N3:id;var N4:id;
	var templ:seq[id];
	var slotT : mid;
	start state init {
		entry {
			N1 = new OpenWSN_Mote(0);
			N2 = new OpenWSN_Mote(1);
			N3 = new OpenWSN_Mote(2);
			N4 = new OpenWSN_Mote(1);
			//initalize the slot machine
			templ.insert(0, N1); templ.insert(0, N2); templ.insert(0, N3); templ.insert(0, N4);
			slotT = new SlotTimerMachine(templ);
			templ.remove(0);templ.remove(0);templ.remove(0);templ.remove(0);assert(sizeof(templ) == 0);
			//initialize the connection
			templ.insert(0, N2);
			send(N1, Initialize, (slotT,templ));
			templ.remove(0);assert(sizeof(templ) == 0);
			templ.insert(0, N1); templ.insert(0, N3); templ.insert(0, N4);
			send(N2, Initialize,(slotT,templ));
			templ.remove(0); templ.remove(0); templ.remove(0); assert(sizeof(templ) == 0);
			templ.insert(0, N2); templ.insert(0, N4);
			send(N3, Initialize, (slotT,templ));
			templ.remove(0); templ.remove(0); assert(sizeof(templ) == 0);
			templ.insert(0, N2); templ.insert(0, N3);
			send(N4, Initialize, (slotT,templ));
			templ.remove(0); templ.remove(0); assert(sizeof(templ) == 0);
			
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
	var myNeighbours: seq[id];
	//preferred neighbour (time parent)
	var myTimeParent:(id, int);
	//last synchronized 
	var lastSynched: int;
	//current slot
	var currentSlot: (bool, (id, id)); //(isshared, id)
	//slot timer
	var slotTimer:mid;
	//local
	var i:int;
	
	start state init_mote {
		defer newSlot;
		ignore Data;
		entry {
			//init the connections
			myRank = (int)payload;
			myTimeParent = (null, 10000);
			lastSynched = 0;	
		
		}
		on Initialize goto WaitForNewSlot
		{ 
			slotTimer = ((mid,seq[id]))payload[0];
			myNeighbours = ((mid,seq[id]))payload[1];
		};
	}

	action CheckOperationTobePerfomed {
		if(myRank != 0)
			lastSynched = lastSynched + 1;
		currentSlot = ((bool, (id,id))) payload;
		temp = OperationTxorRxorSleep();
		if(temp == 0)
			raise(Tx);
		if(temp == 1)
			raise(Rx);
		if(temp == 2)
			raise(Sleep);
	}
	
	model fun OperationTxorRxorSleep() : int {
		if(*)
			return 0; // Tx
		else if (*)
			return 1; // Rx
		else
			return 2; // Sleep
	}
	
	state WaitForNewSlot {
		ignore Data, Ack;
		entry {
		
		}
		on newSlot do CheckOperationTobePerfomed;
		on Tx goto DataTransmissionMode;
		on Rx goto DataReceptionMode;
		on Sleep goto WaitForNewSlot
		{
			send(slotTimer, endSlot);
		};
	}
	
	model fun TransmitData(target:id) {
		if(target == null)
		{
			//choose non-det
			i = sizeof(myNeighbours) - 1;
			while(i>= 0)
			{
				if(*)
				{
					send(myNeighbours[i], Data, (this, myRank));
					return;
				}
				else
					i = i - 1;
			}
		}
		else
		{
			send(target, Data, (this, myRank));
		}
	}
	
	model fun CSMA_CA() : bool {
		if(*)
		{
			return true;
		}
		else
			return false;
	}
	
	state DataTransmissionMode {
		entry {
			if(!currentSlot[0])
			{
				if(currentSlot[1][0] == this)
				{
					TransmitData(currentSlot[1][1]);
					raise(TxDone);
				}
				else
				{
					raise(Local);
				}
			}
			else
			{
				//this slot is shared
				check = CSMA_CA();
				if(check)
				{
					TransmitData(null);
					raise(TxDone);
				}
				else
				{
					raise(Local);
				}
			}
			
		}
		on Local goto WaitForNewSlot
		{
			send(slotTimer, endSlot);
		};
		on TxDone goto WaitForAck;
	}
	
	state WaitForAck {
		ignore Data;
		entry {
		
		}
		on Ack goto WaitForNewSlot
		{
			//update the timeparent
			if(myTimeParent[1] > ((id,int))payload[1])
				myTimeParent = ((id,int))payload;
				
			if(((id,int))payload[0] == myTimeParent[0])
				lastSynched = 0; //Synched
				
			send(slotTimer, endSlot);
		};
		on default goto WaitForNewSlot
		{
			send(slotTimer, endSlot);
		};
	}
	
	state DataReceptionMode {
		entry {
			
		}
		on Data goto WaitForNewSlot
		{
			//Update my preferred parent 
			if(myTimeParent[1] > ((id,int))payload[1])
				myTimeParent = ((id,int))payload;
				
			if(((id,int))payload[0] == myTimeParent[0])
				lastSynched = 0; //synched 
			
			send(((id,int))payload[0], Ack, (this, myRank));
			
			send(slotTimer, endSlot);
		};
	}
	
}

model machine SlotTimerMachine {
	var AllMotes:seq[id];
	var i: int;
	var counter: int;
	start state init{
		entry {
			counter = 0;
			AllMotes = (seq[id])payload;
			raise(Local);
		}
		on Local goto SendNewSlot;
	}
	
	state SendNewSlot {
		entry {
			i = sizeof(AllMotes) - 1;
			while(i>=0)
			{
				send(AllMotes[i], newSlot, (true, (null, null)));
				i = i - 1;
			}
		}
		on endSlot do increaseCounter;
		on Local goto SendNewSlot;
	}
	
	action increaseCounter {
		counter = counter + 1;
		if(counter == sizeof(AllMotes))
		{
			counter = 0;
			raise(Local);
		}
	}
}

