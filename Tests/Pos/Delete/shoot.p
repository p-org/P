event Shot;

main machine Gun {
    var target:id;

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

    state die {
        entry { delete; }
    }
}
