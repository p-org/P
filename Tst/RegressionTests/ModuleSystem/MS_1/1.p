event x;
event y;
event z;
event a;
interface K1 x, y;
interface K2 a, z;
test testName Mod1, hide x in (Mod1, hide a in (Mod2)) refines Mod1;

module Mod1
creates K2
{
	main machine M1
	implements K1
	{
		start state S 
		{
		}
	}
}

module Mod2 {
	machine M2
	implements K2
	{
		start state S 
		{
		}
	}
}