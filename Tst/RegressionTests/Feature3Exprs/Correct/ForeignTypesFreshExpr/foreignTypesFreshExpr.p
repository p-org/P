//Testing foreign types and "fresh" expression
type A;
type B;
type T = (A, B);

main machine X {
    var x1: A;
	var x2: A;
	var y1: B;
	var y2: B;
	var t1: T;
	var t2: T;
	var am: map[A,int];

    start state Start {
	    entry {
			x1 = default(A);
			x2 = fresh(A);
			assert x1 != x2;

			y1 = default(B);
			y2 = fresh(B);
			assert y1 != y2;

			assert t1.0 == x1;
			assert t1.1 == y1;

			am[x1] = 42;
			am[x2] = 43;
			assert am[x1] == 42;
		}   
	}
}
