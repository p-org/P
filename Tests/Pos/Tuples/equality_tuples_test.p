main machine Entry {
    var m:id;

    start state init {
        entry {
            m = new Foo((1,2,3,4));
        }
    }
}

machine Foo {
    var a,b:(int,int);
    var c:int;

    start state dummy {
        entry {
	      a[0] = (((int, int, int, int))payload)[0];
	      a[1] = (((int, int, int, int))payload)[1];
	      b[0] = (((int, int, int, int))payload)[2];
	      b[1] = (((int, int, int, int))payload)[3];
            assert (a != b);
            b = (1,2);
            assert ( a == b);
        }
    }
}
