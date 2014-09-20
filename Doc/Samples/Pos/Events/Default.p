/* HAHAHA
*/
main machine Entry {
    start state init {
        entry {
        }
        on default goto S1;
    }

    state S1 {
        entry {
            assert(trigger == default);
        }
    }
}
