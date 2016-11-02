type compTup1 = (first: int, second: (int, bool));
type slot = (server: int, seqN: int);
type compMap = map[slot, compTup1];

event XYZ : compMap;

machine Main {
	var map1 : compMap;
	var slot1 : slot;
	var slot2 : slot;
	var val1 : compTup1;
	var val2 : compTup1;
	
	start state S1 {
		entry {
			val1.first = 1;
			val1.second.0 = 100;
			val1.second.1 = false;
			
			val2.first = 10;
			val2.second.0 = 1000;
			val2.second.1 = true;
			
			slot1 = (server = 0, seqN = 0);
			slot2 = (server = 1, seqN = 1);
			
			map1[slot1] = val1;
			map1[slot2] = val2;
			
			send this, XYZ, map1;
		}
		on XYZ do (payload: compMap) {
			assert(payload[slot1] == val1);
			assert(payload[slot2] == val1);
		}
	}
}
