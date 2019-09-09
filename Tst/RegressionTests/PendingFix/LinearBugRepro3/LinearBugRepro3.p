machine Main {
    start state User_Init {
        entry {
            var a : int;
            var b : any;

            a = 3;
            b = false;

            b = a swap;
            b = true;

            print "a + 1 = {0}", a + 1;
        }
    }
}
