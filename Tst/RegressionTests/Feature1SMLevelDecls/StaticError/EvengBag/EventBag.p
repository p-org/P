event e: int;

eventBag machine Main {
	var m1: M1;
	var i: int;
	var prev: int;
	start state Init {

		entry {
			m1 = new M1((sender= this, num= 0));
			i = 0;
			prev = 0;
			while (i < 100)
			{
				print format("Waiting for events to pile up {0}", i);
				i = i + 1;
			}
		}
		on e do (p: int) {
			assert p >= prev;
			prev = p;
		}
	}
}

machine M1 {
	var i: int;
	var j: int;
	start state Init {
		entry (payload: (sender: Main, num: int)) {
			print format("sender: {0}", payload.sender);
			i = payload.num;
			j = i + 10;
			while (i < j)
			{
				send payload.sender, e, i;
				i = i + 1;
			}
		}
	}
}