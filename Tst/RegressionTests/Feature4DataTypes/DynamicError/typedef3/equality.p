type F;
type G = F;

main machine MyMachine
{
    start state Init
    {
        entry
        {
            var f : F;
            var g : G;
	    g = fresh(G);
            assert f == g;
        }
    }

}
