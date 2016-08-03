event a;
	main machine Mach1 S{
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
