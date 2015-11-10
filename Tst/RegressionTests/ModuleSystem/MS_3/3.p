event x : MI_1;
event a;
event y;
interface MI_1 y;

test t1 Mod1, Mod2;
implementation Mod1, Mod2;

module Mod1
private a
sends x
creates M2
{
	main machine M1
	receives a, y
	implements MI_1
	{
		var id: machine;
		start state S 
		{
			entry {
				id = new M2();
				send id as M2, x, this;
			}
		}
	}
}

module Mod2
sends y
{
	machine M2
	receives x
	{
		var i2 : int;
		start state S1
		{
			entry {
			
			}
			on x goto S2;
		}
		
		state S2 {
			entry {
				assert(false);
			}
		}
	}
}

module Mod3
private x
sends a

{
	machine M3
	{
		var i3: int;
		start state S1
		{
			entry {
			
			}
			on x goto S2;
		}
		
		state S2 {
			entry {
				assert(false);
			}
		}
	}
}