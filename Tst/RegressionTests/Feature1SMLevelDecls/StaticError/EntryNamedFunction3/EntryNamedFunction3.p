machine Main {
    var m1_machine: machine;
    start state S {
        entry{
            m1_machine = new m1("four");
        }
    }
}

machine m1 {
   	start state S{
   		entry foo;
   	}
    fun foo (){
   		assert payload == 4;
    }
}

