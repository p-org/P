machine Main {
    var m1_machine: machine;
    var m2_machine: machine;

    start state S {
        entry{
            m1_machine = new m1("four");
            m2_machine = new m2((1,2,3))
        }
    }
}

machine m1 {
   	start state S{
   		entry foo;
   	}
    fun foo (payload: int){
   		assert payload == 4;
    }
}

machine m2 {
    start state S{
        entry foo;
    }
 fun foo (payload: map){
        assert payload == (1,2,3);
 }
}

