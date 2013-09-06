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

    start state dummy {
        entry {
            a = (1,2);
            b = (a[0] + 3, a[1]+ 4);
            anull(a);
            assert( a == (1,2));
        }
    }

    foreign fun anull(a:(int,int)):(int,int) {
        a[0] = 0;
        a[1] = 0;
        return a;
    }
}
