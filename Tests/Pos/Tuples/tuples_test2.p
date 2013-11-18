main machine Entry {
    model fun inc(a:int) : int { return a + 1; }
    model fun inc_tup(a:(int, int)):(int, int) {
        return (a[0] + 1, a[1] + 1);
    }
    start state dummy {
        entry {}
    }
}
