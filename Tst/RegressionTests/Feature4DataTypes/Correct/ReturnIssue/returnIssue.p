//XYZing return types
type ArgType = int;
type ResultType = int;

machine Main {
    start state Init
    {

    }

    fun foo()
    {
        var s : ResultType;
		s = bar0();
        s = bar1(1);
		s = bar2(0, 0);
    }

	fun bar0() : ResultType
    {
        return default(ResultType);
    }

    fun bar1(Settings : int) : ResultType
    {
        return default(ResultType);
    }

    fun bar2(Settings : int, Settings1: ArgType) : ResultType
    {
        return 0 + default(ArgType);
    }
}
