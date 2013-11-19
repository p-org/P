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

    start state dummy {
        entry {
            c = dot(sadd((1,2), 1), (5,6));
            assert(c == 28);
        }
    }

    model fun sadd(a:(int,int), b:int):(int,int) {
        a[0] = a[0] + b;
        a[1] = a[1] + b;
        return a;
    }

    model fun dot(a:(int,int), b:(int, int)):int {
        return a[0] * b[0] + a[1] * b[1];
    }

    model fun anull(a:(int,int)):int {
        a[0] = 0;
        a[1] = 0;
        return 0;
    }
}
