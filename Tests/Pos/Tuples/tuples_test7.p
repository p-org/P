main machine Entry {
    var m:id;

    start state init {
        entry {
            m = new Foo();
        }
    }
}

machine Foo {
    var a:(bool,int);

    start state dummy {
        entry {
            a = (false, 1);
        }
    }
}
