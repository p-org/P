// Regression: an integer literal larger than Int32 must produce a clean,
// located "value is out of range" diagnostic (exit code 1), NOT crash the
// compiler with an OverflowException escaping the type checker.
machine Main {
    start state Init {
        entry {
            var a : int;
            a = 9999999999999;
        }
    }
}
