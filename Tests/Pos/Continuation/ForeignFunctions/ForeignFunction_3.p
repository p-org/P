//Model function with New operation in it

event dummy;

machine Real {
var ghostm :mid;
var setme : int;
start state init {
	entry { assert(setme == 1);}
	}
}

main model machine Ghost {
    var local:int;
	var real : id;
	model fun createMachine(): int
	{	
		real = new Real(ghostm = this, setme = 1);
		return 1;
	}
	
    start state Ghost_Init {
        
		entry {
			local =createMachine();
			assert(local == 1);
        }
       
    }
}
