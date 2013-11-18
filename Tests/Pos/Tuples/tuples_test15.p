main machine Entry {
    var m:id;

    start state init {
        entry {
            m = new Foo();
        }
    }
}

machine Foo {
    start state dummy {
        entry {
            vadd((1,2), (5,6));
        }
    }

    foreign fun vadd(a:(int,int), b:(int, int)):(int, int) {
        return (a[0] + b[0] , a[1] + b[1]);
    }
}
