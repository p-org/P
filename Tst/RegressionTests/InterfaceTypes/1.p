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
	start state M {
		entry {
			inter = new L();
			send inter, x;
		}
		on x do {};
		on y do {};
	}
}