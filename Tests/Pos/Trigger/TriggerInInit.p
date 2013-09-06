event EFoo:int;

main machine Entry {
    start state S1 {
        entry {
            assert(trigger != EFoo);
        }
    }   
}
