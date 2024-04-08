// Tests the correctness of set types

machine Main {
    var s: set[int];
	
    start state S
    {
       entry
       {
       		var i: int;
       		var total: int;
       		s += (1);
       		s += (2);
       		s += (2);
       		s += (3);
       		s += (4);

       		while(i < sizeof(s))
       		{
       			total = total + s[i];
       			i = i + 1;
       		}
       		assert total == 10;
       }
    }
}

