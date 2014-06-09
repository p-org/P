event message:int;
event unit:int;

main machine GodMachine 
\begin{GodMachine}
    var PongMachine: id;
	var temp_NM : id;
	var currentNumber:int;
    start state Init {
	    entry {
			//Let me create my own sender/receiver
			sendPort = new SenderMachine((nodemanager = null, param = null));
            receivePort = new ReceiverMachine((nodemanager = null, param = null));
            send(receivePort, hostM, this);	
			currentNumber = 0;
			raise(unit, 0);
	    }
		on unit goto TestNow;
	}
	
	state TestNow {
		entry {
			_SEND(receivePort, message, currentNumber);
		}
		on message goto TestNow 
		{
			assert(currentNumber == (int)payload);
			currentNumber = currentNumber + 1;
		};
	}
\end{GodMachine}