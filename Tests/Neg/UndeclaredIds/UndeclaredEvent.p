main model machine Foo {
    start state S1 {
        entry {
        }
        on Bar goto S2;
    }   

    state S2 {
        entry { }
    }
}
