main machine Entry {
    submachine Foo {
        start state Bar {
            entry {
                assert(payload == null);
                assert(trigger == null);
            }
        }
    }
}
