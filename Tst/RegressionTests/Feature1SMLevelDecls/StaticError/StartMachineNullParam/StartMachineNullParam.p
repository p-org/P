machine Main {
    start state Init {
        entry (payload: int) {};
        exit {
            assert true;
        }
    }
}
