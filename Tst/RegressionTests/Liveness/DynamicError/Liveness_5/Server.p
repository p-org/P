event Local;
event Search : int;
event SearchStarted;
event SearchFinished;

machine Client
{
	start state Init
	{
		entry (payload : machine)
		{
			send payload, Search, 30;
		}
	}
}

machine Main
{
	var store : seq[int];

	start state Init
	{
		entry
		{
			store = default(seq[int]);
			store += (0, 8);
			store += (1, 18);
			store += (2, 28);

			raise Local;
		}
		on Local goto Waiting;
	}

	state Waiting
	{
		entry
		{
			new Client(this);
		}
		on Search do (payload : int)
		{
			var flag : bool;
			var index : int;

			announce SearchStarted;
			flag = false;
			index = 0;
			while(index < sizeof(store) && flag == false)
			{
				if (payload == store[index])
				{
					flag = true;
				}
				index = index + 1;
			}

			if(flag == true)
			{
				announce SearchFinished;
				raise halt;
			}
			else
			{
				send this, Search, payload;
			}
		}
	}
}

spec Liveness observes SearchStarted, SearchFinished
{
	start state Init
	{
		entry
		{
			goto Searched;
		}
	}

	cold state Searched
	{
		on SearchStarted goto Searching;
	}

	hot state Searching
	{
		on SearchFinished goto Searched;
		on SearchStarted goto Searching;
	}
}
