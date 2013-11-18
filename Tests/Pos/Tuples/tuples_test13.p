main machine Entry {
    var m:id;

    start state init {
        entry {
            m = new Foo();
        }
    }
}

machine Foo {
    var c:int;
    var t:(int,int);

    start state dummy {
        entry {
            t= sadd((1,2), 1);
            c = dot(t, (5,6));
            assert(c == 28);
        }
    }

    foreign fun sadd(a:(int,int), b:int):(int,int) {
        a[0] = a[0] + b;
        a[1] = a[1] + b;
        return a;
    }

    foreign fun dot(a:(int,int), b:(int, int)):int {
        return a[0] * b[0] + a[1] * b[1];
    }

    foreign fun anull(a:(int,int)):int {
        a[0] = 0;
        a[1] = 0;
        return 0;
    }
}
