// Regression: a tuple field index larger than Int32 must produce a clean,
// located "value is out of range" diagnostic (exit code 1), NOT crash the
// compiler with an OverflowException from parsing the field number.
machine Main {
    start state Init {
        entry {
            var t : (int, int);
            var x : int;
            t = (1, 2);
            x = t.99999999999;
        }
    }
}
