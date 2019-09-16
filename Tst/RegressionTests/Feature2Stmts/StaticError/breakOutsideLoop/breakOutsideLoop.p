machine Main {
    start state Init {
        entry {
            var i: int;
            // Include a loop to ensure we actually check whether the break statement
            // is *inside* a loop, not just whether a loop is *present*.
            while (i < 10) {
                i = i + 1;
            }
            break;
        }
    }
}