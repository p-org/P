//XYZs complex data types involving enum Types in assign/remove/insert errors): sequences, tuples, maps
//XYZs casting error on line 131

event E assert 1;
event E1 assert 1;
event E2 assert 1;
event E3 assert 1;

enum Foo { foo0, foo1, foo2, foo3, foo4 }
enum Bar { bar0, bar1, bar2, bar3 }

type Tuple = (x: Foo, y: Bar);
type SeqFoo = seq[Foo];
type SeqTuple = seq[Tuple];
type MapIntSeqFoo = map[int, SeqFoo];
type MapIntSeqTuple = map[int, SeqTuple];
type MapIntFoo = map[int,Foo];
type MapIntTuple = map[int,Tuple];

machine Main {
	var v1, v3: SeqFoo;
	var v2: SeqTuple;
	var t : (a: seq [Foo], b: MapIntSeqFoo);
	var t1: Tuple;
	var t0: MapIntFoo;
	var t2: MapIntSeqFoo;
	var t3: (x: SeqTuple, y: MapIntFoo);
	var s1: seq[SeqFoo];
	var s4: seq[SeqTuple];
	var s2: seq[MapIntFoo];
	var s3: seq[MapIntTuple];
	var m2: map[int,MapIntFoo];
	var s6: seq[any];
	var i: int;
	var mac: machine;
	
    start state S
    {
       entry
       {
	      /////////////////////////default expression for enum type:
		  assert t1.x == default(Foo);
          assert t1.y == default(Bar);
          assert t1.x == foo0;
          assert t1.y == bar0;
		  assert t1 == default(Tuple);
		
		  assert v1 == default(SeqFoo);
		  assert sizeof(v1) == 0;
		
		  /////////////////////////tuples of enum type:
		  t1 = (x = foo1, y = bar2);
		  assert t1.x == foo1 && t1.y == bar2;
		
		  t1.x = foo4;
		
		  t1 = baz();
		  //assert t1.x == default(Foo) && t1.y == default(Bar);
		
	      /////////////////////////sequences with enum type:
		  v1 = default(SeqFoo);
		  v1 += (0,foo1);
		  v1 += (0,foo2);
		  assert v1[0] == foo2;                    //holds
		  assert v1[0] == foo2 && v1[1] == foo1;   //holds
		  v3 = v1;
		  v1 -= (1);
		
		  v1 += (0, foo3);
		  v1 += (0, foo4);
		  v1 -= (1);
		
		  v1 = default(SeqFoo);
		  v1 += (0,foo1);
		  v1[0] = foo2;
		  v1 -= (foo() - 1);         //OK
		
		  /////////////////////////sequence of enum type as payload:
		  v1 = default(SeqFoo);
		  v1 += (0,foo1);
          v1 += (0,foo3);
	      mac = new XYZ(v1);
		
		  /////////////////////////map with sequences of enum type:
		  v1 = default(SeqFoo);
		  v1 += (0,foo1);
		  v1 += (1,foo2);
		  assert v1[0] == foo1 && v1[1] == foo2;        //holds
		  t2[0] = v1;
		  assert t2[0][0] == foo1 && t2[0][1] == foo2;    //holds
		  assert 0 in t2;                                //holds
		
		  i = keys(t2)[0];
		  assert(i == 0);                               //holds
		  assert values(t2)[0][0]== foo1;                //holds
		
		  assert sizeof(t2) == 1;                     //holds
		  t2 -= (0);
		  assert sizeof(t2) == 0;                     //holds
		
		  v3 = default(SeqFoo);
		  v3 += (0,foo3);
		  v3 += (1,foo4);
		  t2[0] = v1;
		  t2[1] = v3;	
		  assert sizeof(t2) == 2;                     //holds
		  assert t2[1][0] == foo3 && t2[1][1] == foo4;    //holds
		  t2 -= (1);
		  assert sizeof(t2) == 1;                     //holds
		
		  ////////////////////////sequence of maps (casting any => map[int, SeqFoo] is involved)
		  //s6: seq[any];
		  //var t2: MapIntSeqFoo;
		  //type MapIntSeqFoo = map[int, SeqFoo];
		  v1 = default(SeqFoo);
		  v1 += (0,foo1);
		  v1 += (1,foo2);
		  t2[0] = v1;
		  v3 = default(SeqFoo);
		  v3 += (0,foo3);
		  v3 += (1,foo4);
		  t2[1] = v3;
		
		  s6 = default(seq[any]);
		  s6 += (0,t2);                        //OK
		
		  //t2[1] = s6;                       //error: invalid casting
		  t2[0][0] = s6[0] as Foo;          //dynamic error: "value must be a member of type"

		  ////////////////////////////map of sequences of enum type
		  v1 = default(SeqFoo);
		  v1 += (0,foo1);
		  v1 += (1,foo2);
		  v3 = default(SeqFoo);
		  v3 += (0,foo3);
		  v3 += (1,foo4);
		
		  t2 = default(MapIntSeqFoo);
		  t2[0] =  v1;
		  t2[1] = v3;
		
		  assert sizeof(t2) == 2;            //holds
		  assert t2[0][0] == foo1 && t2[0][1] == foo2;   //holds
		  assert t2[1][0] == foo3 && t2[1][1] == foo4;   //holds
		
		  ////////////////////////tuple with sequence and map:
		  //var t0: MapIntFoo;
		  t1 = default(Tuple);
		  t1 = (x = foo3, y = bar0);
		  v2 = default(SeqTuple);
		  v2 += (0,t1);
		
		  t0[0] = foo0;
		  t0[1] = foo1;
		  t0[2] = foo2;
		  t0[3] = foo3;
		  assert t0[0] == foo0 && t0[1] == foo1 && t0[2] == foo2 && t0[3] == foo3;   //holds
		
		  //var t3: (x: SeqTuple, y: MapIntFoo);
		  v2 = default(SeqTuple);
		  v2 += (0,t1);
		  t3 = (x = v2, y = t0);
		  assert t3.x[0].x == foo3;           //holds
		  assert t3.y[0] == foo0;             //holds
		  assert sizeof(t3.x) == 1 && sizeof(t3.y) == 4;            //holds
		
       }
    }

    fun foo() : int
    {
       return 1;
    }

	fun baz() : Tuple
	{
		return (x = default(Foo), y = default(Bar));
	}
}

machine XYZ {
	var ss: SeqFoo;
	start state init {
		entry (payload: SeqFoo) {
		    ss = payload;
			//assert(ss[0] == foo3);            //holds
		}	
	}
}
