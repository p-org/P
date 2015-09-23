event x;
event y;
event z;
event a;
interface K x, y;
interface K2 x, y, z, a;

main machine L 
implements K
{
	var inter: machine;
	var inter1: K2;
	start state M {
		entry {
			inter = new L();
			inter1 = inter as K;
			send inter, a;
			send inter1, a;
		}
		on x do {};
		on y do {};
	}
}