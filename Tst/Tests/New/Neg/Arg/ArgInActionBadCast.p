event EFoo:int;
event EBar:bool;

main model Foo {
    var a:int; 
    var b:bool;

    start state S1 {
        entry {
        }

        on EBar do IntAct;
    }   

    action IntAct {
        a = payload as int;
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
