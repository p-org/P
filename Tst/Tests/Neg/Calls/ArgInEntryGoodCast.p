event EFoo:int;
event EBar:bool;

main model machine Foo {
    var a:int; 
    var b:bool;

    start state S1 {
        entry {
            call(S2);
        }

        on EFoo goto S2;
        on EBar goto S3;
    }   


    state S2 {
        entry {
            a = (int)payload;
        }
    }

    state S3 {
        entry {
            b = (bool)payload;
        }
    }
}
