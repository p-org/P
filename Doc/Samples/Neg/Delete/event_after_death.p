event Shot;

main machine Gun {
    var target:id;

    start state init {
        entry {
            target = new Target();
            send(target, Shot);
            send(target, Shot);
			assert(false);
        }
    }
}

machine Target {
    start state init {
        on Shot goto die;
    }

    state die {
        entry { raise(delete); }
    }
}
