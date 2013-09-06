main machine Entry {
    var a:int;
    var b:int;

    foreign fun bar(a:int, b:int) {
        assert(a+b == -5);
        assert(a*b == 6);
        assert(a * -b == -6);
        assert(-a * b == -6);
        assert(-a + b == -1);
        assert(a + -b == 1);
    }

    start state foo {
        entry {
            a = -2;
            b = -3;
            bar(a,b);
            bar(-2, -3);
        }
    }
}
