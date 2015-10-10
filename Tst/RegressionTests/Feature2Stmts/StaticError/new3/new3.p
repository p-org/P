event x;
event y;


main machine God
{
	var p : (event, event);
	var m : machine;
	start state S {
		entry {
			
			p = (x, y);
			m = new Test(p);
			new God(p);
			new Test(null);
			new Test((4, x));
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