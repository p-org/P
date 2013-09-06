main machine Entry {
    var m:mid;
    start state init {
        entry {
            m = new Foo(a=(1,1));
        }
    }
}

machine Foo {
    var a:(any, any);
    start state s { }
}
