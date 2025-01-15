event e: int;
event e2: int;

eventchannel machine Main {
	var m1: M1;
	var m2: M2;
	var i: int;
	var count1: int;
	var count2: int;
	start state Init {
		
		entry {
			count1 = 0;
			count2 = 10;
			m1 = new M1((sender= this, num= count1));
			m2 = new M2((sender= this, num= count2));
			i = 0;
			while (i < 10)
			{
				print format("Waiting {0}", i);
				i = i + 1;
			}
		}
		on e do (p: int) {
			assert p == count1;
			count1 = count1 + 1;
		}
		on e2 do (p: int) {
			assert p == count2;
			count2 = count2 + 1;
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

machine M2 {
	var i: int;
	var j: int;
	start state Init {
		entry (payload: (sender: Main, num: int)) {
			print format("sender: {0}", payload.sender);
			i = payload.num;
			j = i + 10;
			while (i < j)
			{
				send payload.sender, e2, i;
				i = i + 1;
			}
		}
	}
}
