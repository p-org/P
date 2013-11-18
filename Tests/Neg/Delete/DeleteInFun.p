event Shot;

main machine Gun {
    var target:mid;

    start state init {
        entry {
            target = new Target();
            send(target, Shot);
        }
    }
}

machine Target {
    start state init {
        on Shot goto die;
    }

    model fun baz() { delete; }

    state die {
        entry { baz(); }
    }
}
