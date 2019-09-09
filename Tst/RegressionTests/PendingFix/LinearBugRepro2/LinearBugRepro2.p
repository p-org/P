machine Main {
    start state User_Init {
        entry {
            var a : (x: int);
            var b : (x: any);

            a = (x=3,);
            b = (x=false,);

            b = a swap;
            b.x = true;

            print "a.x + 1 = {0}", a.x + 1;
        }
    }
}
