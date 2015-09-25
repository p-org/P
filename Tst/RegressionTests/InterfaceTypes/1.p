event x;
event y;
event z;
event a;
interface K x, y;

main machine L 
implements K
{
	var inter: K;
	var dumb : event;
	var xx : machine;
	start state M {
		entry {
			xx = this;
			inter = new L();
			send inter, x;
			send xx as K, x;
		}
		on x do {};
		on y do {};
	}
}