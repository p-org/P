event x;
main machine TestMachine {
	var x1 : int;
	var m : map[int, int];
	var s : seq[int];
	var t : (int);
	var nt : (item1 : int, item2 : bool);
	start state Init {
		entry {
			t = (1, ) as (int);
			nt = (item1 = 1, item2 = false);
			nt.item1 = 100;
			s += (1,100);
			s -= (0);
		}
	}
	

}
