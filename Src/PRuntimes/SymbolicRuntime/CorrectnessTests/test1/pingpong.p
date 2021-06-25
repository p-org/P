event ePing;
event ePong;
event eDing;
event eDong;

machine Main {
	start state Init {
		entry {
			var x1, x2: machine;
			x1 = new Receiver();
			x2 = new Receiver();
			new Sender1(x1);
			new Sender2(x2);
			new Sender3(x1);
			new Sender4(x2);
		}
	}
}

machine Sender1 {

	start state Init {
		entry (rec: machine){
			send rec, ePing;
			send rec, ePing;
		}

	}
}

machine Sender2 {

	start state Init {
		entry (rec: machine){
			send rec, eDong;
			send rec, eDong;
		}

	}
}

machine Sender3 {

	start state Init {
		entry (rec: machine){
			send rec, ePong;
			send rec, ePong;
		}

	}
}

machine Sender4 {

	start state Init {
		entry (rec: machine){
			send rec, eDing;
			send rec, eDing;
		}

	}
}


machine Receiver {
	start state Init {
		defer ePing, ePong, eDing, eDong;
	}
}