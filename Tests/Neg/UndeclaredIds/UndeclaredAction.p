event EFoo;

main ghost machine Foo {
    start state S1 {
        entry {
        }
        on EFoo do bar;
    }   
}
