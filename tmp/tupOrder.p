machine M {
    start state S1 {
    }
}

machine N {
    start state S1 {
        entry {
            var a : M;
            a = new M();
        }
    }
}
