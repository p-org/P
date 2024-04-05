machine Main
sends eD0Entry, eD0Exit;
{
  var Driver: machine;

	start state User_Init {
		entry{
			Driver = new OSRDriverInterface();
			raise(eUnit);
		}
		on eUnit goto S0;
	}
	
	state S0 {
		entry{
			send Driver, eD0Entry;
			raise(eUnit);
		}
		on eUnit goto S1;
	}
	
	state S1 {
		entry {
			send Driver, eD0Exit;
			raise(eUnit);
		}
		on eUnit goto S0;
	}

}



	
	
		
