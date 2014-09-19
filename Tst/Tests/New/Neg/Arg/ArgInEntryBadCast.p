event EFoo:int;
event EFoz:model;
event EBar:bool;
event EBaz:event;

main model Foo {
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
            b = payload as bool;
        }
    }

    state S3 {
        entry {
            a = payload;
        }
    }
}
