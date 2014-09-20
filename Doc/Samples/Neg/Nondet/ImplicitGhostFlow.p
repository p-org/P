main machine Entry {
  var a:bool;
    var b:bool;

    start state init {
        entry {
            a = * || *;
            if (a) {
                b = true;
            } else {
                b = false;
            }
        }
    }
}
