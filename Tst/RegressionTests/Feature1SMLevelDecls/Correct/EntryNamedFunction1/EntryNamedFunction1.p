machine Main {
    var m1_machine: machine;
    var m2_machine: machine;
    var m3_machine: machine;
    var m4_machine: machine;

    var msg_map : map[int, seq[string]];
    var seqOfMsgs: seq[string];
    var msg_tuple: (int, bool, string);

    start state S {
        entry{
            m1_machine = new m1(4);
            msg_tuple = (1,true,"msg1");
            m2_machine = new m2(msg_tuple);           
            seqOfMsgs += (0,"Hello");
            seqOfMsgs += (1,"World");
            msg_map += (1,seqOfMsgs);
            m3_machine = new m3(msg_map);
        }
    }
}

machine m1 {
   	start state S{
   		entry foo1;
   	}
    fun foo1 (payload: int){
   		assert payload == 4;
    }
}

machine m2 {
    start state S{
        entry foo3;
    }
    fun foo3 (payload: (int, bool, string)){
        assert payload == (1,true,"msg1");
    }
}


machine m3 {
    start state S{
        entry foo4;
    }
    fun foo4 (payload: map[int, seq[string]]){
        assert payload[1][0] == "Hello";
    }
}

