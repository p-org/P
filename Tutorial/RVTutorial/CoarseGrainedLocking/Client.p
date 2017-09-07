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
		var val: seq[int];
		var i: int;
		i = 0;
		while (i < iter) {
            x = AcquireLock(lock, this);
            x = val swap;
			Process(val swap);
            x = val swap;
			ReleaseLock(lock, x move);
			i = i + 1;
		}
	}

	fun Process(val: seq[int]) {
		val += (0, 1);
	}
}
