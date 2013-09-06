main machine Entry {
    var a, b:(any, any);

    start state init {
        entry {
            a = (4, false);
            b = (4, (1,2));
            b = a;
        }
    }
}
