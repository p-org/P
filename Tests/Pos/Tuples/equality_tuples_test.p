main machine Entry {
    var m:mid;

    start state init {
        entry {
            m = new Foo(a=(1,2), b=(3,4));
        }
    }
}

machine Foo {
    var a,b:(int,int);
    var c:int;

    start state dummy {
        entry {
            assert (a != b);
            b = (1,2);
            assert ( a == b);
        }
    }
}
