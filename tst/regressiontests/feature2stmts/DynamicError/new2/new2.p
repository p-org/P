event x;
event y;


main machine God
{
	var m : machine;
	start state S {
		entry {
			var p : (event, event);
			p = (x, y);
			m = new Test(p);
		}
	}
}

machine Test : (event, any)
{
	start state S
	{
		
		entry {
			var p: (event, int);
			p = payload as (event, int);
			assert(p.0 == x);
		}
	}
}