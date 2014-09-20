//Model function with New operation in it

event dummy;

machine Real {
var ghostm :mid;
var setme : int;
start state init {
	entry { ghostm = (((mid,int))payload)[0]; setme = (((mid,int))payload)[1]; assert(setme == 1);}
	}
}

main model machine Ghost {
    var local:int;
	var real : id;
	model fun createMachine(): int
	{	
		real = new Real((this, 1));
		return 1;
	}
	
    start state Ghost_Init {
        
		entry {
			local =createMachine();
			assert(local == 1);
        }
       
    }
}
