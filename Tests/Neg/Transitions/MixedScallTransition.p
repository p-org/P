event EFoo;

ghost main machine Foo {
    var a:int;

    start state S1 {
        entry {
            call(S2);
        }
        on EFoo goto S2;
    }   

    state S2 {
        entry {
        }
    }
}
