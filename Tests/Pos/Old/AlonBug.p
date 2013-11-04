event E;

main machine Program {
	start state Init {
		entry { raise (E); }
		exit { assert (false); }
		on E push Call;
	}

	state Call {
		entry { raise (E); }
	}
}
