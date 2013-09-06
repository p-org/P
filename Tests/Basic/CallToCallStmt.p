main machine Entry {
    start state Start {
        entry { }
    }

    foreign fun id(a:int):int {
        return a;
    }

    // Some tests to make sure Call is correctly transformed to CallStmt in all
    // contexts.

    foreign fun foo(a:int):int {
        id(a);
        if (2 > id(1))  {
            id(a);
            assert(false);
        } else {
            id(a);
        }

        while (2 < id(1)) {
            id(a);
        }

        while (2 < id(1)) {
            id(a);
            assert(false);
        }
        id(a);
        id(a);
    }
}
