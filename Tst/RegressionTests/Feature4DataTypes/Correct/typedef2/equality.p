type F = bool;
type G = F;

machine Main {
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
