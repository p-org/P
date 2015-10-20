event x : K1;
event y;
event z;
event a;
interface K1 a;
interface K2 y, x;

test t1 Mod1, Mod2;
test t2 Mod2;

module Mod1
private a
sends x
creates K1, K2
{
	main machine M1
	implements K1
	{
		var id: machine;
		start state S 
		{
			entry {
				id = new K2();
				send id as K2, x, this;
			}
		}
	}
}
/*
spec Mon monitors x 
{
	start state S1
	{
		entry {
		
		}
	}
}
*/
module Mod2
sends y
{
	machine M2
	implements K2
	{
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