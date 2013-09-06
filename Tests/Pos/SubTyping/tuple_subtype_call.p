event Bar:(int,int);

main machine Entry {
    var a:(int, int);
    var b:(any, any);

    foreign fun switch(a:(any,any)):(any,any) {
        return (a[1], a[0]);
    }

    start state dummy {
        entry {
            b = switch((1,2));
            raise(Bar, b);
        }

        on Bar goto S1;
    }

    state S1 {
        entry {
            a = ((int, int)) payload;
            assert(a == (2,1));
        }
    }
}
