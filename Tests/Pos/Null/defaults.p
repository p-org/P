main machine Entry {
    var a:mid;
    var b:eid;
    var c:(int,eid);
    var d:((mid, eid), (eid, bool));

    start state Foo {
        entry {
            assert( a == null );
            assert( b == null );
            assert( c == (0, null) );
            assert( d == ((null, null), (null, false)) );
        }
    }
}
