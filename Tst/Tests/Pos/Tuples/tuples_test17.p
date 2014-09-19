main machine Entry {
    var a:(int, (bool, int));
    var b:(f0:(int, bool), f1:(int, int, (g0:int, g1:bool)));

    start state dummy {
        entry {
            a = (1, (true, 10));
            b = (f1=(2,1,(g1=true, g0=5)), f0=(7, false));
        }
    }
}
