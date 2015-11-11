event a;
	main machine Mach1
	{
		start state S {
			entry {
				receive {
					case a : {}
				}
				
				receive {
					case a : {}
				}
			}
		}
	}
