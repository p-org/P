extern machine M();
event e1 : seq[int];

machine M {
    start state S1 {}
    fun foo(x: int) {
        var y: bool;
        y = true;
    }
}
