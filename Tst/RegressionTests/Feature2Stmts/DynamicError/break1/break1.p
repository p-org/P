machine Main {
    start state Init {
        entry {
            var i: int;
            while (i < 100) {
                i = i + 1;
                if (i >= 50)
                    break;
            }
            assert i != 50; // Should fail
        }
    }
}