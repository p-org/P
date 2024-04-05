
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
		t = "e";
		v = "ek";
		assert s>t;
		assert t<s;
		assert s>=t;
		assert s>=s;
		assert s<=s;
		assert v>t;
		assert v>=t;
		s = "";
		assert s<v;
		assert s<=v;
	    }
	}
}
