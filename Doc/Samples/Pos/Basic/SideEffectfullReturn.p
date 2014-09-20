main machine Entry {
    var a:(int,int);

    model fun foo():(int,int) {
        return (1,2);
    }

    start state Init {
        entry {
            a=foo();
            assert(a == (1,2));
        }
    }
}
