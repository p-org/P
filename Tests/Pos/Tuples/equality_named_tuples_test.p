main machine Entry {
    var m:id;

    start state init {
        entry {
            m = new Foo();
        }
    }
}

machine Foo {
    var a,b:(f1:int,f2:int);
    var c:int;

    start state dummy {
        entry {
            a = (f1=3, f2=4);
            b = (f2=4, f1=3);
            assert ( a == b);
        }
    }
}
