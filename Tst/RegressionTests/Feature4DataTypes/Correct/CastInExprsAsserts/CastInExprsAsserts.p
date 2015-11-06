//Tests cast operator in expressions (valid casts)
//Tests static errors
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

main machine M
{    
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
		  //y = a as int;             //dynamic error: "value must be a member of type" (other test)
		  
		  a = 1;
		  y = a as int;             //OK
		  assert (y == a);           //holds	  
		  ////////////////////////// bool vs any:
		  a = default(any);
		  //b = a as bool;             //dynamic error: "value must be a member of type" (other test)
		  a = true;
		  b = a as bool;             //OK
		  assert (b == a);           //holds
		  ////////////////////////// event vs any:
		  a = default(any);
		  assert (a == null);        //holds
		  ev = a as event;           //OK
		  assert(a == ev);           //holds
		  a = E;
		  ev = a as event;             //OK
		  assert (ev == E);           //holds
		  ////////////////////////// machine vs any:
		  a = default(any);
		  assert (a == null);        //holds
		  mac = default(machine);
		  assert (mac == null);        //holds
		  mac = a as machine;           //OK
		  assert (mac == a);            //holds
		  a = new Test();
		  mac = a as machine;           //OK
		  assert (mac == a);           //holds
		  ////////////////////////// map vs any:
		  a = default(any);
		  //m1 = a as map[int,int];    //dynamic error: "value must be a member of type" (other test)
		  m1[0] = 1;
		  m1[1] = 2;
		  a = m1;                      //OK
		  assert (a == m1);            //holds
		  ////////////////////////// seq vs any:
		  a = default(any);
		  //s = a as seq[int];         //dynamic error: "value must be a member of type" (other test)
		  s += (0, 1);
          s += (1, 2);
		  a = s;                      //OK
		  assert (a == s);            //holds
		  ////////////////////////// tuple vs any:
		  a = default(any);
		  //ts = a as (a: int, b: int);    //dynamic error: "value must be a member of type" (other test)
		  ts.a = 1;
		  ts.b = 2;
		  a = ts;
		  assert (a == ts);             //holds
		  
		  a = default(any);
		  //tt = a as (int, int);    //dynamic error: "value must be a member of type" (other test)
		  tt.0 = 1;
		  tt.1 = 2;
		  a = tt;
		  assert (a == tt);         //holds
		  ////////////////////////// Casts in event payload:
		  /////////////////////////////////////// int payload:
		  y = 1;
		  send mac, EI1, y;   //OK
		  a = 1;
		  send mac, EI6, a as int;  //OK
		  /////////////////////////////////////// tuple payload:
		  ts1.a = 1;
		  ts1.b = true;
		  send mac, ET1, ts1;
		  send mac, ET2, (a = 2, b = false);
		  /////////////////////////////////////// seq payload:
		  s = default(seq[int]);
		  s += (0, 1);
          s += (1, 2);
		  send mac, ESEQ1, s;                 //OK
		  
		  s1 = s;
		  send mac, ESEQ2, s1 as seq[int];  //OK

		  /////////////////////////////////////// map payload:
		  m1 = default(map[int,int]);
		  send mac, EMAP1, m1;       //OK
		  m1[0] = 1;
		  m1[3] = 3;
		  send mac, EMAP11, m1;       //OK
		  
		  m9 = default(map[int,any]);
		  send mac, EMAP2, m9 as map[int,int];   //OK, but dynamic error will follow
		  m9 = m1;
		  send mac, EMAP3, m9 as map[int,int];   //OK
		  
		  raise halt;
       }    
    }       
}

machine Test {
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
		on EI1 push testEI1;
		on EI6 push testEI6;
		on ET1 push testET1;
		on ET2 push testET2;
		on ESEQ1 push testESEQ1;
		on ESEQ2 push testESEQ2;
		on EMAP1 push testEMAP1;
		on EMAP11 push testEMAP11;
		on EMAP2 push testEMAP2;
		on EMAP3 push testEMAP3;
	}
	// int is sent
	state testEI1 {
		entry (payload: any) {
			ta = payload;
			assert(ta == 1);           //holds
			//yt = payload as int;       //dynamic error: "value must have a concrete type" (TODO: add Sent\Test.p) (no error in runtime!)
			//assert(yt == 1);           //holds?
			pop;
		}
	}
	// "any as int" is sent
	state testEI6 {
		entry (payload: int) {
			yt = payload as int;        //OK
			assert(yt == 1);           //holds
			yt = payload;               //OK
			assert(yt == 1);           //holds
			ta = payload as any;       //OK
			assert(yt == 1);           //holds
			pop;
		}
	}
	// tuple is sent via a var
	state testET1 {
		entry (payload: (a: int, b: bool)) {
			tts1 = payload;    //OK
			assert (tts1.a == 1 && tts1.b == true);   //holds
			tts1 = payload;                          //OK
			assert (tts1.a == 1 && tts1.b == true);   //holds
			pop;
		}
	}
	// tuple is sent via literal
	state testET2 {
		entry (payload: (a: int, b: bool)) {
			tts1 = payload;    //OK
			assert (tts1.a == 2 && tts1.b == false);   //holds
			pop;
		}
	}
	// seq[int] sent
	state testESEQ1 {
		entry (payload: seq[int]) {	
			s = payload;    //OK
			assert (s[0] == 1);          //holds
			s = payload;                //OK
			assert (s[0] == 1);          //holds
			
			s1 = payload;    //OK
			assert (s1[0] == 1);          //holds
			s1 = payload;                //OK
			assert (s1[0] == 1);          //holds
			
			s1 = payload as seq[any];    //OK
			assert (s1[0] == 1);          //holds
			pop;
		}
	}
	// "seq[any] as seq[int]" is sent
	state testESEQ2 {
		entry (payload: seq[int]) {	
			s = payload;    //OK
			assert (s[0] == 1);          //holds
			s = payload;                //OK
			assert (s[0] == 1);          //holds
			
			s1 = payload;    //OK
			assert (s1[0] == 1);          //holds
			s1 = payload;                //OK
			assert (s1[0] == 1);          //holds
			
			s1 = payload as seq[any];    //OK
			assert (s1[0] == 1);          //holds
			pop;
		}
	}
	// default(map[int,int]) is sent
	state testEMAP1 {
		entry (payload: map[int,int]) {
			mi = payload;     
			//assert (mi[0] == 0);  //dynamic error: "key not found" (TODO)
			mi[0] = 0;
			mi[3] = 3;
			assert (mi[0] == 0 && mi[3] == 3);                  //holds
			
			mi = default(map[int,int]);
			mi = payload;
			//assert (mi[0] == 0);  //dynamic error: "key not found" (TODO)
			
			ma = payload;
			//assert (ma[0] == 0);  //dynamic error: "key not found" (TODO)
			ma = default(map[int,any]);
			ma = payload;
			//assert (ma[0] == 0);  //dynamic error: "key not found" (TODO)
			ma = default(map[int,any]);
			
			ma = payload as map[int,any];
			//assert (ma[0] == 0);  //dynamic error: "key not found" (TODO)	
			pop;
		}
	}
	// map[int,int] is sent (0,1) (3,3)
	state testEMAP11 {
		entry (payload: map[int,int]) {
			mi = default(map[int,int]);
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
			pop;
		}
	}
	// default(map[int,any]) is sent as map[int,int]
	state testEMAP2 {
		entry (payload: map[int,int]) {
			mi = payload;             //OK
			//assert (mi[0] == 1 && mi[3] == 3);  //dynamic error: "key not found" (TODO)
			
			mi = default(map[int,int]);
			mi = payload;  //OK
			//assert (mi[0] == 1 && mi[3] == 3);  //dynamic error: "key not found" (TODO)
			
			ma = payload;   //ok
			//assert (ma[0] == 1 && ma[3] == 3);  //dynamic error: "key not found" (TODO)
			
			ma = default(map[int,any]);
			ma = payload;                     //OK
			//assert (ma[0] == 1 && ma[3] == 3);  //dynamic error: "key not found" (TODO)
			ma = default(map[int,any]);
		
			ma = payload as map[int,any];     //OK
			//assert (ma[0] == 1 && ma[3] == 3);  //dynamic error: "key not found" (TODO)
            			
			pop;			
		}
	}
	// map[int,any] assigned a value of  map[int,int] type is sent as map[int,int]
	state testEMAP3 {
		entry (payload: map[int,int]) {
			mi = payload;             //OK
			assert (mi[0] == 1 && mi[3] == 3);  //holds
			mi = default(map[int,int]);
			mi = payload;  //OK
			assert (mi[0] == 1 && mi[3] == 3);  //holds
			
			ma = payload;   //ok
			assert (ma[0] == 1 && ma[3] == 3);  //holds
			ma = default(map[int,any]);
			ma = payload;                     //OK
			assert (ma[0] == 1 && ma[3] == 3);  //holds
			ma = default(map[int,any]);
			
			ma = payload as map[int,any];     //OK
			assert (ma[0] == 1 && ma[3] == 3);  //holds
			pop;
		}
	}
}
