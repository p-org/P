main machine Entry {
    var a:(any, any);
    var b:any;
    var c:any;
    start state dummy {
        entry {
            a = (1, 1);
            b = (1, 1);
            c = a;
            assert(b == c);
        }
    }
}
