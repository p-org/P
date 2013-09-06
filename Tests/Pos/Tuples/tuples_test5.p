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
            b = (a[0] + 3, a[1]+ 4);
            c = 2;
            b = addScalar(a, c);
        }
    }

    foreign fun addScalar(a:(int,int), b:int):(int,int) {
        return (a[0] + b, a[1] + b);
    }
}
