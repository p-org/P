event EFoo:int;
event EBar:bool;

main machine Entry {
    var a:int; 
    var b:bool;

    start state S1 {
        entry {
        }

        on EFoo goto S2;
        on EBar goto S3;
    }   


    state S2 {
        entry {
            a = payload;
        }
    }

    state S3 {
        entry {
            b = payload;
        }
    }
}
