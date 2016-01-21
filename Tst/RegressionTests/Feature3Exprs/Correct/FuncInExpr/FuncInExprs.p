event x;
event y;

main machine TestM {
	start state Init {
		entry {
			if(!F2().Success || F1().Success) //problem : the problem is with || of multiple function calls. I am not sure why.
			//if(!F2().Success) //no problem
			{
				assert(false);
			}
		}
	}
	fun F2() : (Success: bool)
	{
		if(F1().Success)
			return F1();
			
	}
	fun F1() : (Success: bool)
	{
		var ret : (Success: bool);
		ret.Success =  true;
		return ret;
	}
}