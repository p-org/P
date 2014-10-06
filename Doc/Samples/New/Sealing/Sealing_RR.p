// No error if RR or RunToComp delaying scheduler is used.


event m1 : machine;
event m2 : machine;
event unit;

main machine Machine1 {
	fun seal() [invokescheduler = seal] {}
    fun unseal() [invokescheduler = unseal] {}
	
	
	var lastReceivedFrom : machine;
	var target1 : machine;
	var target2 : machine;
	
	start state Init {
		entry {
			target1 =  new Machine2(this);
			target2 =  new Machine2(this);
			raise(unit);
		}
		
		on unit goto Wait;
	}
	
	state Wait {
		entry {
			
		}
		on m1 goto WaitAgain with { lastReceivedFrom = payload; };
	}
	
	state WaitAgain {
		on m1 goto Wait with {
			assert (lastReceivedFrom == payload);
			send payload, m2, this;
		};
		on m1 goto Wait with {
			assert (lastReceivedFrom == payload);
			send payload, m2, this;
		};
	
	}

}


machine Machine2 {
	fun seal() [invokescheduler = seal] {}
    fun unseal() [invokescheduler = unseal] {}
	
	var target : machine;
	
	start state Init {
		entry {
			seal();
			target = payload as machine;
			send target, m1, this;
			send target, m1, this;
			unseal();
			raise(halt);
		}
	}

}
