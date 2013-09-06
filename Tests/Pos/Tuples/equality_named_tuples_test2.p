main machine Entry {
    var m:mid;

    start state init {
        entry {
            m = new Foo();
        }
    }
}

machine Foo {
    var a:(f1:int,f2:int); //These two are the same type actually..
    var b:(f2:int,f1:int);
    var c:int;

    start state dummy {
        entry {
            a = (f2=4, f1=3); // Order in constructors doesnt matter too
            b = (f1=3, f2=4);
            assert ( a == b);
            b = a;
            assert ( a == b);
            b = (f2=10, f1=11);
            assert ( a != b);
        }
    }
}
