event x;

machine Xsender {
	start state Init {
		entry {
			return 3;
			send this, x;
		}
	}
}