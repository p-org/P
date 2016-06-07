//Testing static function return type
type foo = int;

fun Foo() : (foo, int) {
       return 0;
}

main model MainMachine
{
    start state Init
    {
    }
}
