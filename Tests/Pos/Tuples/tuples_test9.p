main machine Entry {
    var m:mid;

    start state init {
        entry {
            m = new Foo();
        }
    }
}

machine Foo {
    var a,b:((int, int),int);

    start state dummy {
        entry {
            a = ((1,2), 3);
            if ( ((5 , true) , 6)[0][1] ) {
                a = ((3,4), 5);
            } else {
                assert(false);
            }
        }
    }
}
