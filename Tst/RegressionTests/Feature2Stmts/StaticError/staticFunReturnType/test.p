//Testing static function return type
type foo = int;

static fun Foo() : (foo, int) {
       return 0;
}

main model MainMachine
{
    start state Init
    {
    }
}
