main machine Entry {
    var m:id;

    start state init {
        entry {
            m = new Foo();
        }
    }
}

machine Foo {
    var a:(int, int);
    var c:int;

    start state dummy {
        entry {
            a = sadd((1,2), 1);
            a = vadd(a, (5,6));
            anull(a);
            assert(a == (7, 9));
        }
    }

    foreign fun sadd(a:(int,int), b:int):(int,int) {
        a[0] = a[0] + b;
        a[1] = a[1] + b;
        return a;
    }

    foreign fun vadd(a:(int,int), b:(int, int)):(int, int) {
        return (a[0] + b[0] , a[1] + b[1]);
    }

    foreign fun anull(a:(int,int)):int {
        a[0] = 0;
        a[1] = 0;
        return 0;
    }
}
