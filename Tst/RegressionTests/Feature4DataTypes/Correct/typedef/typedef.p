type compTup1 = (first: int, second: (int, bool));
type compTup2 = (first: compTup1, second: seq[compTup1]);

event XYZ : compTup2;

machine Main {
	var compVal2 : compTup2;
	var compVal1 : compTup1;
	start state S1 {
		entry {
			compVal1.first = 1;
			compVal1.second.0 = 100;
			compVal1.second.1 = false;
			
			compVal2.first = compVal1;
			compVal2.second += (0, compVal1);
			
			send this, XYZ, compVal2;
		}
		on XYZ do (payload: compTup2) {
			assert(payload.first.first == 1);
			assert(payload.first.second.0 == 100);
			assert(payload.first.second.1 == false);
			assert(sizeof(payload.second) == 1);
		}
	}
}
