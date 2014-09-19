event Bar;

main machine Entry {
    var a:mid;
    var b:eid;
    var c:(int,eid);
    var d:((mid, eid), (eid, bool));

    start state Foo {
        entry {
            a = null;
            b = null;
            c = (5, null);
            d = ((null, Bar), (null, true));
 
            assert( a == null );
            assert( b == null );
            assert( c == (5, null) );
            assert( d == ((null, Bar), (null, true)) );
        }
    }
}
