main machine Entry {
    var a:(int, int);
    var b:int;
    var c:(int, int);

    start state init {
        entry {
            b = -1;
            a = (5,5);
            a[0] = b + a[0];
            assert(a[0] == 4);
        }
    }
}
