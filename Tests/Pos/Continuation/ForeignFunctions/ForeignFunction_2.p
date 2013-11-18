//Model function with Non-Det operation in it
//Non-det with recursive calls inside function

main model machine Ghost {
    var local:int;
	var nondetval : bool;
	model fun inc(a:int): int
	{	
		if(a >= 102)
			return a;
		a = inc(a + 1);
		return a;
	}
	model fun nondetinc (a:int) : int {
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
			local = 99;
			local = nondetinc(local);
			assert(local == 102 && nondetval || local == 102 && !nondetval );
        }
       
    }
}
