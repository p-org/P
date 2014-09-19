main machine Entry {
    var a:(t1:any, t2:any);
	var b:(t1:int, t2:any);
    start state init {
        entry {
            a = (t1=4, t2=false);
            b = (t1=4, t2=2);
            a = b;
        }
    }
}
