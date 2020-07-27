type Cycle0 = (int, Cycle1);
type Cycle1 = (Cycle2, bool);
type Cycle2 = (Cycle0, Cycle1);

type A = (B, C);
type NA = (b: B, c: C);
type B = int;
type C = bool;

type SeqB = seq[B];
type MapBC = map[B,C];

machine Main {
	var a: A;
	var c: C;
    start state S0 {
		entry {
			c = a.1;
			a.0 = a.0 + 1;
		}
	}
	
	var na: NA;
	state S1 {
		entry {
			c = na.c;
			na.b = na.b - 1;		
		}
	}

	var sb: SeqB;
	state S2 {
		entry {
			sb += (0,0);	
		}
	}

	var mbc: MapBC;
	state S3 {
		entry {
			mbc[0] = true;
		}
	}
}
