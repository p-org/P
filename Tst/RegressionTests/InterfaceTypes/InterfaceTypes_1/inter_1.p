event x;
event y;
event z;
event a;
event b;

//interface decls
interface XY x, y;
interface XYA x, y, a;
interface XB x, b;


//module declaration
//M inputs - outputs
module M
receives x, y
sends x
creates XY, XYA
{
	
	main machine test
	implements XY
	{
		var inter: machine;
		var dumb : event;
		var xx : XY;
		var m : map[int, int];
		start state M11 {
			entry {
				inter = new test();
				xx = inter as XY;
				send xx, a;
			}
		}
	}
}