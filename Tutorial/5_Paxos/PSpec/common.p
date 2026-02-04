// Unreliably send event `e` with payload `p` to every `target`
fun UnreliableBroadcast(target_machines: set[machine], e: event, payload: any) {
	var i: int;
	while (i < sizeof(target_machines)) {
		if (choose()) {
			send target_machines[i], e, payload;
		}
		i = i + 1;
	}
}

// Unreliably send event `e` with payload `p` to every `target`, potentially multiple times
fun UnreliableBroadcastMulti(target_machines: set[machine], e: event, payload: any) {
	var i: int;
	var n: int;
	var k: int;

	while (i < sizeof(target_machines)) {
		// Each message is sent `k` is that number of times 
		k = choose(3);
		// Times we've sent the packet so far
		n = 0;
		while (n < k) {		
			send target_machines[i], e, payload;
			n = n + 1;
		}
		i = i + 1;
	}
}

// Reliably send event `e` with payload `p` to every `target`
fun ReliableBroadcast(target_machines: set[machine], e: event, payload: any) {
	var i: int;
	while (i < sizeof(target_machines)) {
		send target_machines[i], e, payload;
		i = i + 1;
	}
}

// Reliably send event `e` with payload `p` to a majority of `target`. Unreliable send to remaining, potentially multiple times.
fun ReliableBroadcastMajority(target_machines: set[machine], e: event, payload: any) {
	var i: int;
	var n: int;
	var k: int;
	var majority: int;

	majority = sizeof(target_machines) / 2 + 1;

	while (i < sizeof(target_machines)) {
		// Each message is sent `k` is that number of times 
		k = 1;
		if (i >= majority) {
			k = choose(3);
		}
		// Times we've sent the packet so far
		n = 0;
		while (n < k) {		
			send target_machines[i], e, payload;
			n = n + 1;
		}
		i = i + 1;
	}
}
