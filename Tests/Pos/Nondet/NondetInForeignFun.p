main machine Entry {
    var pw, i, res, a:int;

    foreign fun RandomNum(nBits:int):int {
        i = 0;
        pw = 1;
        res = 0;
        
        while (i < nBits) {
            if (*) res = res + pw;
            pw = pw * 2;
            i = i + 1;            
        }

        return res;
    }

    start state init {
        entry {
            a = RandomNum(9);
        }
    }
}
