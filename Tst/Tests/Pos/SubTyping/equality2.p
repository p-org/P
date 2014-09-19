main machine Entry {
    var a:(any, any);
    var b:any;
    var c:any;
    start state dummy {
        entry {
            a = (1, 1);
            b = (1, 1);
            assert(a == b);
            assert(a == (1,1));
            assert(b == a);
            assert((1,1) == a);

            b = (1,2);
            assert(a != b);
            assert(a != (1,2));
            assert(b != a);
            assert((1,2) != a);
        }
    }
}
