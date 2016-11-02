event unit;
event dummy;
machine Main {
	var x : int;
	var otherMachine: machine;
	start state Init {
		entry {
			x = 0;
			otherMachine = new Other();
			raise(unit);
		}
		on unit goto XYZ;
	}
	
	state XYZ {
		entry {
			send otherMachine, dummy;
			raise unit;
		}
		on unit goto XYZ;
	}
	
}

machine Other {
	var count : int;
	start state Init {
		
		entry {
			count = count + 1;
		}
		on dummy goto Init;
	}
}

