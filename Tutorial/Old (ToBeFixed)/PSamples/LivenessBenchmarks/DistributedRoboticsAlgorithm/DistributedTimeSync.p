event eGetTimePeriod: (robotid: int, source: machine);
event eCurrTimePeriod : int;

fun MotionPrimitiveTimePeriod() : int { return 500; /*0.5 ms*/ }

fun GetCurrentTimePeriod(timeSync:machine, robotid: int, source: machine) : int {
	var retVal: int;
	send timeSync, eGetTimePeriod, (robotid = robotid, source = source);

	receive {
		case eCurrTimePeriod: (payload: int) { retVal = payload; }
	}
	return retVal;
}

machine DistributedTimeSyncMachine {
	var robotsLocalTimeV : map[int, int];
	var timerV : machine;
	var noOfRobots: int;
	start state Init {
		entry {
			var index: int;
			noOfRobots = GetNumOfRobots();
			index = 0;
			while(index < noOfRobots)
			{
				robotsLocalTimeV += (index, 0);
			    index = index + 1;
			}
			//create timer
			timerV = new Timer(this);
			
			//start timer
			StartTimer(timerV, MotionPrimitiveTimePeriod());
			goto WaitForGetTimeRequest;
		}
	}

	state WaitForGetTimeRequest {
		on TIMEOUT do {
			var index: int;
			index = 0;
			while(index < noOfRobots)
			{
				robotsLocalTimeV[index] = robotsLocalTimeV[index] + 1;
			    index = index + 1;
			}
			//print ".";
			//start timer
			StartTimer(timerV, MotionPrimitiveTimePeriod());
		}
		on eGetTimePeriod do (payload : (robotid: int, source: machine)) {
			send payload.source, eCurrTimePeriod, robotsLocalTimeV[payload.robotid];
		}

	}
}