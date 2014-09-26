event Unit;

main machine Entry {
    start state init {
        entry { raise(Unit); }
        on Unit do Noop;
    }


    fun Noop() {
        if ($) {
            raise(Unit);
        }
    }
}
