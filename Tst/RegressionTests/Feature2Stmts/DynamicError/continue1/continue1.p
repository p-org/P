machine Main {
    start state Init {
        entry {
            var i: int;
            var num_odd: int;

            while (i < 100) {
                i = i + 1;

                if ((i / 2) * 2 == i)
                    continue;

                num_odd = num_odd + 1;
            }

            assert num_odd != 50; // Should fail
        }
    }
}