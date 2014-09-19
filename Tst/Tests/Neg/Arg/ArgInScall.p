main model machine Foo {
    var a:int;

    start state S1 {
        entry {
            call(S2);
        }
    }   

    state S2 {
        entry {
            a = (int)payload;
        }
    }
}
