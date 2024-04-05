//XYZs cast operator in expressions
//XYZs dynamic error
//Basic types: int, bool, event

event E assert 1: int;
event EI1: int;
event EI2: int;
event EI3: int;
event EI4: int;
event EI5: int;
event EI6: int;
event E1 assert 1;
event E2 assert 1;
event ET1: (a: int, b: bool);
event ET2: (a: int, b: bool);
event ESEQ1: seq[int];
event ESEQ2: seq[int];
event EMAP1: map[int,int];
event EMAP11: map[int,int];
event EMAP2: map[int,int];
event EMAP3: map[int,int];

machine Main {
    var t : (a: seq [int], b: map[int, seq[int]]);
	var t1 : (a: seq [int], b: map[int, seq[int]]);
	var ts: (a: int, b: int);
	var ts1: (a: int, b: bool);
	var tt: (int, int);
	var tbool: (bool, bool);
	var te: (int, event);       ///////////////////////////////////////////////////////////
	var b: bool;
    var y : int;
	var tmp: int;
	var tmp1: int;
	var ev: event;
	var a: any;
	var tmp2: (a: seq [any], b: map[int, seq[any]]);
	var tmp3: map[int, seq[int]];
	var s: seq[int];
    var s1: seq[any];
    var s2: seq[int];
    var s3: seq[seq[any]];
	var s4, s8: seq[(int,int)];
	var s5: seq[bool];
	var s6: seq[map[int,any]];
	var s7: seq[int];
	var s9: seq[event];        /////////////////////////////////////////////////////////
	var s10: seq[any];
	var s11: seq[int];
	var s12: seq[bool];
    var i: int;
	var mac: machine;
	var m1: map[int,int];
	var m4: map[int,int];
	var m3: map[int,bool];
	//TODO: write asgns for m2
	var m5, m6: map[int,any];
	var m2: map[int,map[int,any]];
	var m7: map[bool,seq[(a: int, b: int)]];
	var m8: map[int,event];                    //////////////////////////////////////
	var m9: map[int,any];
	
    start state S
    {
       entry
       {
		  ////////////////////////// int vs any:
		  a = default(any);
		  //y = a as int;             //dynamic error: "value must be a member of type" (other XYZs)
		
		  a = 1;
		  y = a as int;             //OK
		  assert (y == a);           //holds	
		  ////////////////////////// bool vs any:
		  a = default(any);
		  b = a as bool;             //dynamic error: "value must be a member of type" (this XYZ)
		  assert(b == true);
		  raise halt;
       }
    }
}

machine XYZ {
	var ss: seq[int];
    var yt: int;
	var tts1: (a: int, b: bool);
	var tts: (a: int, b: int);
	var ta: any;
	var s: seq[int];
	var s1: seq[any];
	var mi: map[int,int];
	var ma: map[int,any];
	start state init {
		entry {
		    //ss = payload as seq[int];
			//assert(ss[0] == 3);            //holds
		}
		on EI1 goto XYZEI1;
		on EI6 goto XYZEI6;
		on ET1 goto XYZET1;
		on ET2 goto XYZET2;
		on ESEQ1 goto XYZESEQ1;
		on ESEQ2 goto XYZESEQ2;
		on EMAP1 goto XYZEMAP1;
		on EMAP11 goto XYZEMAP11;
		on EMAP2 goto XYZEMAP2;
		on EMAP3 goto XYZEMAP3;
	}
	// int is sent
	state XYZEI1 {
		entry (payload: any) {
			ta = payload as any;
			assert(ta == 1);           //holds
			//yt = payload as int;       //dynamic error: "value must have a concrete type" (TODO: add Sent\XYZ.p) (no error in runtime!)
			//assert(yt == 1);           //holds?
			goto init;
		}
	}
	// "any as int" is sent
	state XYZEI6 {
		entry (payload: int) {
			yt = payload as int;        //OK
			assert(yt == 1);           //holds
			yt = payload;               //OK
			assert(yt == 1);           //holds
			ta = payload as any;       //OK
			assert(yt == 1);           //holds
			goto init;
		}
	}
	// tuple is sent via a var
	state XYZET1 {
		entry (payload: (a: int, b: bool)) {
			tts1 = payload as (a: int, b: bool);    //OK
			assert (tts1.a == 1 && tts1.b == true);   //holds
			tts1 = payload;                          //OK
			assert (tts1.a == 1 && tts1.b == true);   //holds
			goto init;
		}
	}
	// tuple is sent via literal
	state XYZET2 {
		entry (payload: (a: int, b: bool)) {
			tts1 = payload as (a: int, b: bool);    //OK
			assert (tts1.a == 2 && tts1.b == false);   //holds
			goto init;
		}
	}
	// seq[int] sent
	state XYZESEQ1 {
		entry (payload: seq[int]) {	
			s = payload as seq[int];    //OK
			assert (s[0] == 1);          //holds
			s = payload;                //OK
			assert (s[0] == 1);          //holds
			
			s1 = payload as seq[int];    //OK
			assert (s1[0] == 1);          //holds
			s1 = payload;                //OK
			assert (s1[0] == 1);          //holds
			
			s1 = payload as seq[any];    //OK
			assert (s1[0] == 1);          //holds
			goto init;
		}
	}
	// "seq[any] as seq[int]" is sent
	state XYZESEQ2 {
		entry (payload: seq[int]) {	
			s = payload as seq[int];    //OK
			assert (s[0] == 1);          //holds
			s = payload;                //OK
			assert (s[0] == 1);          //holds
			
			s1 = payload as seq[int];    //OK
			assert (s1[0] == 1);          //holds
			s1 = payload;                //OK
			assert (s1[0] == 1);          //holds
			
			s1 = payload as seq[any];    //OK
			assert (s1[0] == 1);          //holds
			goto init;
		}
	}
	// default(map[int,int]) is sent
	state XYZEMAP1 {
		entry (payload: map[int,int]) {
			mi = payload;
			//assert (mi[0] == 0);  //dynamic error: "key not found" (other XYZs)
			mi[0] = 0;
			mi[3] = 3;
			assert (mi[0] == 0 && mi[3] == 3);                  //holds
			
			mi = default(map[int,int]);
			mi = payload as map[int,int];
			//assert (mi[0] == 0);  //dynamic error: "key not found" (other XYZs)
			
			ma = payload as map[int,int];
			//assert (ma[0] == 0);  //dynamic error: "key not found" (other XYZs)
			ma = default(map[int,any]);
			ma = payload;
			//assert (ma[0] == 0);  //dynamic error: "key not found" (other XYZs)
			ma = default(map[int,any]);
			
			ma = payload as map[int,any];
			//assert (ma[0] == 0);  //dynamic error: "key not found" (other XYZs)	
			goto init;
		}
	}
	// map[int,int] is sent (0,1) (3,3)
	state XYZEMAP11 {
		entry (payload: map[int,int]) {
			mi = default(map[int,int]);
			mi = payload;
			assert (mi[0] == 1 && mi[3] == 3);  //holds
			
			mi = default(map[int,int]);
			mi = payload as map[int,int];
			assert (mi[0] == 1 && mi[3] == 3);  //holds
			
			ma = payload as map[int,int];
			assert (ma[0] == 1 && ma[3] == 3);  //holds
			ma = default(map[int,any]);
			ma = payload;
			assert (ma[0] == 1 && ma[3] == 3);  //holds
			ma = default(map[int,any]);
			
			ma = payload as map[int,any];
			assert (ma[0] == 1 && ma[3] == 3);  //holds
			goto init;
		}
	}
	// default(map[int,any]) is sent as map[int,int]
	state XYZEMAP2 {
		entry (payload: map[int,int]) {
			mi = payload;             //OK
			//assert (mi[0] == 1 && mi[3] == 3);  //dynamic error: "key not found" (other XYZs)
			
			mi = default(map[int,int]);
			mi = payload as map[int,int];  //OK
			//assert (mi[0] == 1 && mi[3] == 3);  //dynamic error: "key not found" (other XYZs)
			
			ma = payload as map[int,int];   //ok
			//assert (ma[0] == 1 && ma[3] == 3);  //dynamic error: "key not found" (other XYZs)
			
			ma = default(map[int,any]);
			ma = payload;                     //OK
			//assert (ma[0] == 1 && ma[3] == 3);  //dynamic error: "key not found" (other XYZs)
			ma = default(map[int,any]);
		
			ma = payload as map[int,any];     //OK
			//assert (ma[0] == 1 && ma[3] == 3);  //dynamic error: "key not found" (other XYZs)
            			
			goto init;
		}
	}
	// map[int,any] assigned a value of  map[int,int] type is sent as map[int,int]
	state XYZEMAP3 {
		entry (payload: map[int,int]) {
			mi = payload;             //OK
			assert (mi[0] == 1 && mi[3] == 3);  //holds
			mi = default(map[int,int]);
			mi = payload as map[int,int];  //OK
			assert (mi[0] == 1 && mi[3] == 3);  //holds
			
			ma = payload as map[int,int];   //ok
			assert (ma[0] == 1 && ma[3] == 3);  //holds
			ma = default(map[int,any]);
			ma = payload;                     //OK
			assert (ma[0] == 1 && ma[3] == 3);  //holds
			ma = default(map[int,any]);
			
			ma = payload as map[int,any];     //OK
			assert (ma[0] == 1 && ma[3] == 3);  //holds
			goto init;
		}
	}
}
