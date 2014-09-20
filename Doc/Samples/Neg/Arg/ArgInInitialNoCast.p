main model machine Foo {
    var a:int;

    start state S1 {
        entry {
            a = (int) payload;
        }
    }   
}
