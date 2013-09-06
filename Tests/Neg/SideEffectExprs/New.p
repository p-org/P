main machine Entry {
    var m:mid;
    start state init {
        entry {
            m = (0, new Foo())[1];
        }
    }
}

machine Foo { 
    start state init { }
}
