event CausesError: (LastLogIndex: Idxs, LastLogTerm: Idxs);
type Idxs = (kv: int, config: int, sqr: int);

machine Main {
    start state Init
    {
        entry
        {
        	var x: Idxs;
            print format("{0}", x);
        }
    }

}
