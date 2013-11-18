event SenderId: mid;
event ReceiverId: mid;
event A;
event B;
event Done;

main fair machine Program {
	var sender: mid;
	var receiver: mid;
	start stable state Init {
		entry {
			sender = new Sender();
			receiver = new Receiver();
			send(receiver, SenderId, sender);
			send(sender, ReceiverId, receiver);
		}
	}
}

fair model machine Sender {
	var receiver: mid;
	var done: bool;

	model fun AmIDone(): bool 
	{
		if (*)
			return true;
		else
			return false;
	}

	start state Init {
		on ReceiverId goto Ready;
		exit {
			receiver = (mid) payload;
		}
	}
	
	state Ready {
		entry {
			done = AmIDone();
			if (done) 
				raise(Done);
			else
				send(receiver, A);
		}
		on B goto Ready;
		on Done fair goto Finished;
	}

	stable state Finished {

	}
}

fair model machine Receiver {
	var sender: mid;
	start state Init {
		on SenderId goto Ready;
		exit {
			sender = (mid) payload;
		}
	}
	
	stable state Ready {
		on A goto Respond;
	}

	stable state Respond {
		entry {
			send(sender, B);
		}
		on A goto Respond;
	}
}
