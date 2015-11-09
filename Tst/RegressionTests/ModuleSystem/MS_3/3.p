event x : (event, int);
event a : int;
event y;
interface MI_1 y;

test t1 Mod1;
implementation Mod1;

module Mod1
private a
sends x
{
	main machine M1
	{
		var id: machine;
		start state S 
		{
			entry {
				raise x, (a, 3);
			}
			on x do {
				raise payload.0, payload.1;
			};
		}
	}
}


