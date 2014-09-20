main machine Entry {
    var m:id;

    start state init {
        entry {
            m = new Foo();
        }
    }
}

machine Foo {
    var a,b:(int,int);

    start state dummy {
        entry {
            a = (1,2);
            b = (a[0] + 3, a[1]+ 4);
            b = anull(a);
        }
    }

    model fun anull(a:(int,int)):(int,int) {
        a[0] = 0;
        a[1] = 0;
        return a;
    }

    model fun sup():int { return 0; }
}
