main model Foo {
    var a:int;

    start state S1 {
        entry {
            a = payload as int;
        }
    }   
}
