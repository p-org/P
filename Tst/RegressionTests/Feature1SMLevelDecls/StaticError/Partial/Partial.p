machine A
[partial = null]
{
    state S1
    {
        entry
        {
            x = 42;
            goto S3;
        }
    }

}

machine A
[partial = null]
{
    start state S2 {
        entry {
            goto S1;
        }
    }
    var x:  int;
    state S3 {
        entry {
            assert x == 42;
        }
    }
}

machine Main {
    start state Init {
        entry {
            new A();
        }
    }
}