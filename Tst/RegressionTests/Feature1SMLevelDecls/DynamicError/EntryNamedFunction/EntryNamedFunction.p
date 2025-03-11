event E0;

machine Main {
    var m1_machine: machine;
    var m2_machine: machine;

    start state S {
        entry{
            m1_machine = new m1(4);
            m2_machine = new m2(m1_machine);
        }
    }
}

machine m1 {
   	start state S{
   		entry foo1;

   		on E0 do {
          assert false;
        }
   	}
    fun foo1 (payload: int){
   		assert payload != 4;
    }
    
}


machine m2 {
    start state S{
        entry foo2;
    }
    fun foo2 (payload: machine){
        send payload,E0;
    }
}
