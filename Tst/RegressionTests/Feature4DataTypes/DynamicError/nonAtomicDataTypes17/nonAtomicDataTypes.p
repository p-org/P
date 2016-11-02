//XYZs complex data types in assign/remove/insert errors): sequences, tuples, maps
//Basic types: int, bool, event

event E assert 1; 
event E1 assert 1;
event E2 assert 1;
event E3 assert 1;

machine Main {

	var m11: map[any,any];
	
    start state S
    {
       entry
       {
		  m11 += (null,null);   //error: key must not exist in the map
		  m11 += (1,null);      //OK
		  m11 += (null,1);      //OK: why?
		  raise halt;
       }    
    }
}
