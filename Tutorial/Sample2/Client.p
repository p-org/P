machine Client {
	var lock: LockPtr;
	var iter: int;

	start state Init {
		entry (x: (LockPtr, int)) {
			lock = x.0;
			iter = x.1;
			Work();
		}
	}

	fun Work() {
        var x: any;
		var data: seq[int];
		var i: int;
		i = 0;
		while (i < iter) {
            x = AcquireLock(this, lock);
            x = data swap;
			Process(data swap);
            x = data swap;
			ReleaseLock(lock, x move);
			i = i + 1;
		}
	}

	fun Process(data: seq[int]) {
		data += (0, 1);
	}
}