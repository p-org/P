machine Main {
    start state S {
	    entry {
			var lock: LockPtr;
			var client1: machine;
			var client2: machine;
			var data:seq[int];

			lock = CreateLock(data move);
			client1 = new Client(lock, 3);
			client2 = new Client(lock, 2);
		}
	}
}