main machine Entry {
    start state init {
        entry {
            call(s1);
        }
    }

    state s1 {
        entry {
            //assert(payload == null);
            //assert(trigger == null);
            return;
        }
    }
}
