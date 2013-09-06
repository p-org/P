event EFoo:int;
event EFoz:mid;
event EBar:bool;
event EBaz:eid;

main ghost machine Foo {
    var a:int; 
    var b:bool;

    start state S1 {
        entry {
        }

        on EFoo goto S2;
        on EFoz goto S2;
        on EBar goto S3;
        on EBaz goto S3;
    }   


    state S2 {
        entry {
            b = (bool) payload;
        }
    }

    state S3 {
        entry {
            a = (int) payload;
        }
    }
}
