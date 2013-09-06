main machine Entry {
    var m:mid;

    start state init {
        entry {
            m = new Foo();
        }
    }
}

machine Foo {
    var a:(int,int);
    start state dummy {
        entry {
            if (1<2) {
                a = (1,2);
            } else {
                a = (3,4);
            }
        }
    }
}
