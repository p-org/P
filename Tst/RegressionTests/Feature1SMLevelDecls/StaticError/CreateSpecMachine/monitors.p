event a : int;
event b : bool;

machine Main {
    var model_machine: machine;
    start state Init {
        entry {
            model_machine = new M();
        }
    }

}

spec M observes a {
    start state Init {
        entry {
            raise a, 1;
            raise b, true;
        }
    }
}
