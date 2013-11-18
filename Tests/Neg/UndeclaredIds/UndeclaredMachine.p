event EFoo;

main model machine Foo {
    var m:mid;
    start state S1 {
        entry {
            m = new BAZ();
        }
    }   
}
