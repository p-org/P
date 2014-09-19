main machine Entry {
    var a:bool;
    var b:bool;

    start state init {
        entry {
            a = $ || $;
            b = false;
            push s1;
            assert(b);
        }
    }

    state s1 {
        entry { b = true; return; }
    }
}
