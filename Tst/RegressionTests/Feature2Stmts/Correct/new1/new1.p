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

machine Test : (event, event)
{
	start state S
	{
		
		entry {
			var p: (event, event);
			p = payload as (event, event);
			assert(p.0 == x);
			assert(p.1 == y);
		}
	}
}