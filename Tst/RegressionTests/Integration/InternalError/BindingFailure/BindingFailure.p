// Run by Tst/UnitTests/BindingFailureTest.cs, not by the regression suite.
// The foreign function fails like code compiled against another version of the P runtime does
// (it throws the MissingMethodException that the runtime throws when such code is bound). The
// checker must stop with an internal error that asks to recompile, instead of reporting a bug in
// this (correct) program.
fun CallStaleRuntimeMethod();

machine Main {
  start state Init {
    entry {
      CallStaleRuntimeMethod();
    }
  }
}
