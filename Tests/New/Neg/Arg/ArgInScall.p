main model Foo {
    var a:int;

    start state S1 {
        entry {
            push S2;
        }
    }   

    state S2 {
        entry {
            a = payload as int;
        }
    }
}
