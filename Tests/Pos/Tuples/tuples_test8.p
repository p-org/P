main machine Entry {
    var m:mid;

    start state init {
        entry {
            m = new Foo();
        }
    }
}

machine Foo {
    var a:((int, int, bool),int);

    start state dummy {
        entry {
            a = ((1,2, false), 3);
        }
    }
}
