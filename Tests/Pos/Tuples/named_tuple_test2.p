main machine Entry {
    var a:(fa:int, fb:int);

    model fun inc_nmtup(a:(fa:int, fb:int), c:int):(fa:int, fb:int) {
        return (fa=a.fa + c, fb=a.fb + c);
    }
    start state dummy {
        entry {
            a = (fa=(1-1), fb=3+4);
            a = inc_nmtup(a, 1);
        }
    }
}
