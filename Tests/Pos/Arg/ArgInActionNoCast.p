event EFoo:int;
event EBar:bool;

main machine Entry { 
    var a:int; 
    var b:bool;

    start state S1 {
        entry {
        }

        on EFoo do IntAct;
    }   

    action IntAct {
        a = payload;
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
