event EFoo:int;
event EBar:bool;

main model Foo {
    var a:int; 
    var b:bool;

    start state S1 {
        entry {
        }

        on EFoo do IntAct;
    }   

    fun IntAct() {
        b = arg;
    }

    state S2 {
        entry {
        }
    }

    state S3 {
        entry {
        }
    }
}
