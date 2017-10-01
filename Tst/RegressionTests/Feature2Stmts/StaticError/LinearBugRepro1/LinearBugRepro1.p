machine Main {
    start state User_Init {
        entry {
            var a : int;
            var b : machine;

            a = 3;
            b = new Main();

            print "a = {0}\n", a;
            UnsafeAssign(a swap, b);
            print "a = {0}\n", a + 1;
        }
    }

    fun UnsafeAssign(a : any, b : any) : int {
        a = b;
        return 0;
    }
}
