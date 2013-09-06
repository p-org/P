event EFoo;

main ghost machine Foo {
    var m:mid;
    start state S1 {
        entry {
            m = new BAZ();
        }
    }   
}
