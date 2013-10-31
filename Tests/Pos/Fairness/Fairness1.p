event SenderId: mid;
event ReceiverId: mid;
event A;
event B;

main machine Program {
	var sender: mid;
	var receiver: mid;
	start stable state Init {
		entry {
			sender = new Sender();
			receiver = new Receiver();
			send(sender, ReceiverId, receiver);
			send(receiver, SenderId, sender);
		}
	}
}

fair machine Sender {
	var receiver: mid;
	start state Init {
		on ReceiverId goto Ready;
		exit {
			receiver = (mid) payload;
		}
	}
	
	state Ready {
		entry {
			send(receiver, A);
		}
		on B goto Ready;
	}
}

machine Receiver {
	var sender: mid;
	start state Init {
		on SenderId goto Ready;
		exit {
			sender = (mid) payload;
		}
	}
	
	state Ready {
		on A goto Respond;
	}

	state Respond {
		entry {
			send(sender, B);
		}
		on A fair goto Respond;
	}
}
