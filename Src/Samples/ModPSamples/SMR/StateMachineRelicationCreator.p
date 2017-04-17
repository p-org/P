machine SMRServer
receives;
sends;
{	
	var repFactor : int;
	start state Init {
		entry (payload:(SMRClientInterface, int, bool)){
			var i : int;
			i = 0;
			//rep factor of 3
			repFactor = 3;
			while(i< repFactor)
			{
				CreateSMR_Replica(payload, i);
				i = i + 1;
			}
			raise halt;
		}
	}
	fun CreateSMR_Replica(param: (SMRClientInterface, int, bool), repId: int)
	{
		var smr_rep : machine;
		smr_rep = new SMRServerInterface((client = param.0, reorder = param.2, id = repId));
	}
}
