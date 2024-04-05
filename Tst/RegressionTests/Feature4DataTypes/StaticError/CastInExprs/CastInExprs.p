//XYZs cast operator in expressions
//XYZs static errors; XYZ Correct\CastInExprsAsserts XYZs all asserts
//Basic types: int, bool, event

event E assert 1: int;
event EI1: int;
event EI2: int;
event EI3: int;
event EI4: int;
event EI5: int;
event EI6: int;
event E1: seq[int];
event E2: map[int,int];
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
		  //////////////////////////cast for base types:
		  //////////////////////////int vs bool:
		  y = b as int;         //error: "Cast can never succeed"		
		  b = true;

		
		  b = y as bool;         //error
		  y = 1;

		  //////////////////////////int vs event:
		  y = E as int;         //error
		  y = ev as int;        //error; ev is null
		
		  ev = E;

		
		  y = default(int);
		  ev = y as event;      //error
		
		  y = 3;

		
		  //////////////////////////bool vs event:
		  b = E as bool;         //error
		  b = ev as bool;        //error; ev is null
		
		  ev = E;

		
		  b = default(bool);
		  ev = b as event;      //error
		
		  b= true;

		
		  ////////////////////////// int vs any:
		  a = 1;
		  y = a as int;             //OK
		  assert (y == a);           //holds
		  ////////////////////////// bool vs any:
		  a = true;
		  b = a as bool;             //OK
		  assert (b == a);           //holds
		  ////////////////////////// event vs any:
		  a = default(any);
		  ev = a as event;           //OK
		  a = E;
		  ev = a as event;             //OK
		  assert (ev == a);           //holds
		  ////////////////////////// machine vs any:
		  a = default(any);
		  assert (a == null);        //holds
		  mac = default(machine);
		  assert (mac == null);        //holds
		  mac = a as machine;           //OK
		  assert (mac == a);            //holds
		  a = new XYZ();
		  mac = a as machine;           //OK
		  assert (mac == a);           //holds
		
		  ////////////////////////// map vs seq:
		  s += (0, 1);
          s += (1, 2);
		  m1 = s as map[int,int];   //error	
		  m1[0] = 1;
		  m1[1] = 2;
		  s = m1 as seq[int];      //error
		  ////////////////////////// any vs map:
		  a = default(any);
		  m1 = a as map[int,int];    //dynamic error: "value must be a member of type"
		  a[0] = 1;                  //error
		  a += (0,1);                 //error
		  m1[0] = 1;
		  m1[1] = 2;
		  a = m1;                      //OK

		  ////////////////////////// seq vs any:
		  a = default(any);
		  s = a as seq[int];         //dynamic error: "value must be a member of type"


		  s += (0, 1);
          s += (1, 2);
		  a = s;                      //OK
		  a += (0,2);                   //error
		  ////////////////////////// tuple vs any:
		  a = default(any);
		  ts = a as (a: int, b: int);    //dynamic error: "value must be a member of type"
		  ts.a = 1;
		  ts.b = 2;
		  a = ts;
		  a.a = 0;                  //error
		
		  a = default(any);
		  tt = a as (int, int);    //dynamic error: "value must be a member of type" ?
		  tt.0 = 1;
		  tt.1 = 2;
		  a = tt;
		  a.1 = 0;                  //error
		
		  ////////////////////////// tuple vs map:
		  ts.b = 1;
		  ts.a = ts.b + 1;
		  m1 = ts as map[int,int];   //error
		  tt = (0,0);
		  tt = (1,1);
		  m1 = tt as map[int,int];   //error
		  m1[0] = 1;
		  m1[1] = 2;
		  tt = m1 as (int, int);     //error

		  ////////////////////////// tuple vs seq:
		  ts.b = 1;
		  ts.a = ts.b + 1;
		  s = ts as seq[int];   //error
		  tt = (0,0);
		  tt = (1,1);
		  s = tt as seq[int];   //error
		  s = default(seq[int]);
		  s += (0, 1);
          s += (1, 2);
		  tt = s as (int, int);     //error

		  ////////////////////////// Casts in event payload:
		  /////////////////////////////////////// int payload:
		  y = 1;
		  send mac, EI1, y;   //OK
		  send mac, EI2, b;   //error
		  send mac, EI2, b as int;   //error
		  send mac, EI3, ev;   //error

		  send mac, EI4, mac;   //error
		  send mac, EI4, mac as int;   //error
		  a = null;
		  send mac, EI5, a;    //error
		  a = 1;
          send mac, EI6, a;    //error
		  send mac, EI6, a as int;  //OK
		  /////////////////////////////////////// tuple payload:
		  ts1.a = 1;
		  ts1.b = true;
		  send mac, ET1, ts1;         //OK
		  send mac, ET2, (a = 2, b = false);  //OK
		  /////////////////////////////////////// seq payload:
		  s = default(seq[int]);
		  s += (0, 1);
          s += (1, 2);
		  send mac, ESEQ1, s;                 //OK
		
		  s1 = s;
		  send mac, ESEQ2, s1;               //error
		  send mac, ESEQ2, s1 as seq[int];  //OK
		
		  /////////////////////////////////////// map payload:
		  m1 = default(map[int,int]);
		  send mac, EMAP1, m1;       //OK
		  m1[0] = 1;
		  m1[3] = 3;
		  send mac, EMAP11, m1;       //OK
		
		  m9 = default(map[int,any]);
		  send mac, EMAP2, m9;   //error
		  send mac, EMAP2, m9 as map[int,int];   //OK, but dynamic error will follow
		  m9 = m1;

		  send mac, EMAP3, m9 as map[int,int];   //OK
		
		  raise halt;
       }
    }
}

machine XYZ {
	var ss: seq[int];
	var yt: int;
	var bt: bool;
	var ta: any;
	var tts1: (a: int, b: bool);
	var tts: (a: int, b: int);
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
		entry (payload: int) {
			yt = payload;
			assert(yt == 1);        //holds
			bt = payload as bool;   //error
			goto init;
		}
	}
	// "any as int" is sent
	state XYZEI6 {
		entry (payload: int) {
			yt = payload;              //OK
			assert(yt == 1);           //holds
			yt = payload;              //OK
			assert(yt == 1);           //holds	
			ta = payload as any;       //OK
			assert(yt == 1);           //holds
			goto init;
		}
	}
	// tuple is sent via a var
	state XYZET1 {
		entry (payload: (a: int, b: bool)) {
			tts1 = payload;    //OK
			assert (tts1.a == 1 && tts1.b == true);   //holds
			tts1 = payload;                          //OK
			assert (tts1.a == 1 && tts1.b == true);   //holds
			tts = payload as (a: int, b: int);    //error
			goto init;
		}
	}
	// tuple is sent via literal
	state XYZET2 {
		entry (payload: (a: int, b: bool)) {	
			tts1 = payload as (a: int, b: int);    //error
			tts1 = payload;    //OK
			assert (tts1.a == 2 && tts1.b == false);   //holds
			goto init;
		}
	}
	// seq[int] sent
	state XYZESEQ1 {
		entry (payload: seq[int]) {	
			s = payload;    //OK
			assert (s[0] == 1);          //holds
			s = default(seq[int]);
			s = payload;                //OK
			assert (s[0] == 1);          //holds
			
			s1 = payload;    //OK
			assert (s1[0] == 1);          //holds
			s1 = default(seq[any]);
			s1 = payload;                //OK
			assert (s1[0] == 1);          //holds
			
			s1 = default(seq[any]);
			s1 = payload as seq[any];    //OK
			assert (s1[0] == 1);          //holds
		}
	}
	// "seq[any] as seq[int]" is sent
	state XYZESEQ2 {
		entry (payload: seq[int]) {	
			s = payload;    //OK
			assert (s[0] == 1);          //holds
			s = default(seq[int]);
			s = payload;                //OK
			assert (s[0] == 1);          //holds
			
			s1 = payload;    //OK
			assert (s1[0] == 1);          //holds
			s1 = default(seq[int]);
			s1 = payload;                //OK
			assert (s1[0] == 1);          //holds
			
			s1 = default(seq[int]);
			s1 = payload as seq[any];    //OK
			assert (s1[0] == 1);          //holds
			
			s1 = default(seq[int]);
			s1 = payload as seq[bool];    //error
		}
	}
	// default(map[int,int]) is sent
	state XYZEMAP1 {
		entry (payload: map[int,int]) {
			mi = payload;
			assert (mi[0] == 0);  //dynamic error: "key not found"
			mi[0] = 0;
			mi[3] = 3;
			assert (mi[0] == 0 && mi[3] == 3);                  //holds
			
			mi = default(map[int,int]);
			mi = payload;
			assert (mi[0] == 0);  //dynamic error: "key not found"
			
			ma = payload;
			assert (ma[0] == 0);  //dynamic error: "key not found"
			ma = default(map[int,any]);
			ma = payload;
			assert (ma[0] == 0);  //dynamic error: "key not found"
			ma = default(map[int,any]);
			
			ma = payload as map[int,any];
			assert (ma[0] == 0);  //dynamic error: "key not found" 		
		}
	}
	// map[int,int] is sent
	state XYZEMAP11 {
		entry (payload: map[int,int]) {
			mi = payload;
			assert (mi[0] == 1 && mi[3] == 3);  //holds
			
			mi = default(map[int,int]);
			mi = payload;
			assert (mi[0] == 1 && mi[3] == 3);  //holds
			
			ma = payload;
			assert (ma[0] == 1 && ma[3] == 3);  //holds
			ma = default(map[int,any]);
			ma = payload;
			assert (ma[0] == 1 && ma[3] == 3);  //holds
			ma = default(map[int,any]);
			
			ma = payload as map[int,any];
			assert (ma[0] == 1 && ma[3] == 3);  //holds
		}
	}
	// default(map[int,any]) is sent as map[int,int]
	state XYZEMAP2 {
		entry (payload: map[int,int]) {
			mi = payload;             //OK
			assert (mi[0] == 1 && mi[3] == 3);  ////dynamic error: "key not found"
			mi = default(map[int,int]);
			mi = payload;  //OK
			assert (mi[0] == 1 && mi[3] == 3);  ////dynamic error: "key not found"
			
			ma = payload;   //ok
			assert (ma[0] == 1 && ma[3] == 3);  ////dynamic error: "key not found"
			ma = default(map[int,any]);
			ma = payload;                     //OK
			assert (ma[0] == 1 && ma[3] == 3);  ////dynamic error: "key not found"
			ma = default(map[int,any]);
			
			ma = payload as map[int,any];     //OK
			assert (ma[0] == 1 && ma[3] == 3);  ////dynamic error: "key not found"		
		}
	}
	// map[int,any] assigned a value of  map[int,int] type is sent as map[int,int]
	state XYZEMAP3 {
		entry (payload: map[int,int]) {
			mi = payload;             //OK
			assert (mi[0] == 1 && mi[3] == 3);  //holds
			mi = default(map[int,int]);
			mi = payload;  //OK
			assert (mi[0] == 1 && mi[3] == 3);  //holds?
			
			ma = payload;   //ok
			assert (ma[0] == 1 && ma[3] == 3);  //holds
			ma = default(map[int,any]);
			ma = payload;                     //OK
			assert (ma[0] == 1 && ma[3] == 3);  //holds
			ma = default(map[int,any]);
			
			ma = payload as map[int,any];     //OK
			assert (ma[0] == 1 && ma[3] == 3);  //holds		
		}
	}
}
