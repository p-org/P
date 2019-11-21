
machine Main {
	var vAny: any;
	var vInt: int;
	var s: string;
	var t: string;
	start state S
    {
       entry
       { 	
			vAny = 1;
			vInt = 0;
			s = "a0";
			t = s + "a1";
			assert s != t; //holds
			assert t != "a0a1"; //fails

	   }
	}
}
