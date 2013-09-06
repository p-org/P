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
            a = (1,2);
            b = add(a, 10);
            c = dot(a,b);
        }
    }

    foreign fun add(a:(int,int), b:int):(int,int) {
        a[0] = a[0] + b;
        a[1] = a[1] + b;
        return a;
    }

    foreign fun dot(a:(int,int), b:(int, int)):int {
        return a[0] * b[0] + a[1] * b[1];
    }
}
