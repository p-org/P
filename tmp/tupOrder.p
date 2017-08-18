machine N {
    start state S1 {
        entry {
            var x, y : int;
            x = 4;
            y = 5;
            add(x, x swap);
        }
    }

    fun add(x : int, y : int) : int {
        x = x + y;
        y = 3;
        return x + y;
    }
}