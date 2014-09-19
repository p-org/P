main machine Entry {
    var m:id;
    start state init {
        entry {
            m = new Foo((1,1));
        }
    }
}

machine Foo {
    var a:(any, any);
	    start state s { entry { a = ((any, any))payload; } }
}
