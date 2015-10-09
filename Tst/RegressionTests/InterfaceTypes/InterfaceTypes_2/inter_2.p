event x;
event y;
event z;

interface K x, y;
interface L x, z;

module M 
{
	main machine test
	implements K
	{
		var inter1 : machine;
		start state S 
		{
			entry {
				inter1 = new L(this);
				
			}
			on z do { assert(false); };
		}
	}
	
	machine tester
	implements L
	{
		var test : K;
		start state S
		{
			entry {
				test = payload as K;
				send test, z;
			}
		}
	}

}
