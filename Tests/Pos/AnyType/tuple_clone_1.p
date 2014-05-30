main machine Entry {
	var b:(id, any);
	var c:(any,any);
    start state init {
        entry {
            b = (this, 2);
            c = b;
			b = ((id, any))c;
        }
    }
}
