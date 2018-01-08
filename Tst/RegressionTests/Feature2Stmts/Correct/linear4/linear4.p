machine Main {
    fun upTo(x:int, bound:int) : bool {
        x = x + 1;
        return x < bound;
    }

    start state S {
    }

    fun testWhile() {
        var x: int;
        x = 0;
        while(upTo(x swap, 10)) {
            print "x = {0}", x;
        }
    }
}