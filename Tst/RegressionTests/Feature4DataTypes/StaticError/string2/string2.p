
event E assert 1; 

machine Main {
	var vAny: any;
	var vEvent: event;
	var vInt: int;
	var s:string;
	var t:string;
	var v:string;
	
	start state S
	{
       entry
	    { 	
		vAny = 1;
		s = "h";
		t = s + "k";
		assert "hk"==t;
		v = "a{0}l{1}", s, "l";
		assert v == "ahll";
		t = "{0} {1}", v; //errors
		vInt = -1;
		v = v % vInt; errors
			
	    }
	}
}
