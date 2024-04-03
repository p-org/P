event x;
event y;

machine Main {
	start state Init {
		entry {
			if (!F2().Success || F1().Success)
			{
				assert(false);
			}
		}
	}
	fun F2() : (Success: bool)
	{
		if(F1().Success)
			return F1();
		return default((Success: bool));
	}
	fun F1() : (Success: bool)
	{
		var ret : (Success: bool);
		ret.Success =  true;
		return ret;
	}
}
