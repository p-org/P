event EFoo;

main model machine Foo {
    start state S1 {
        entry {
        }
        on EFoo goto S2;
    }   
}
