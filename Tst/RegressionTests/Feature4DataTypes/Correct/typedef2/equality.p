model type F = bool;
type G = F;

main machine MyMachine
{
    start state Init
    {
        entry
        {
            var f : F;
            var g : G;
            assert f == g;
        }
    }

}
