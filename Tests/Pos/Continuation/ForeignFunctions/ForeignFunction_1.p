//Foreign function with Non-Det operation in it
//Non-det without recursive calls

main ghost machine Ghost {
    var local:int;
	var nondetval : bool;
	foreign fun inc(a:int): int
	{
		return a + 1;
	}
	foreign fun nondetinc (a:int) : int {
		if(*)
		{
			a = inc(a);
			nondetval = true;
		}
		else
		{
			a = inc(a);
			a = inc(a);
			nondetval = false;
		}
		return a;
	}
    start state Ghost_Init {
        
		entry {
			local = 100;
			local = nondetinc(local);
			assert(local == 101 && nondetval || local == 102 && !nondetval );
        }
       
    }
}
