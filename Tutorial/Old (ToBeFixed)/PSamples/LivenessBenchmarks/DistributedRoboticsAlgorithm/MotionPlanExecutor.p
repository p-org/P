event ePlanExecutorMachine: machine;
event eStartExecutingPlan : TimedTrajType;

fun InvokeStartExecutingPathAndWait(path: seq[int], startT: int, robotId: int, timesync: machine)
{

	var currTimePeriod: int;
	var expectedEndTime: int;
	currTimePeriod = GetCurrentTimePeriod(timesync, robotId, this);
	expectedEndTime = sizeof(path) + currTimePeriod;

	//start executing path
	StartExecutingPath(path,startT,robotId);
	
	currTimePeriod = GetCurrentTimePeriod(timesync, robotId, this);
	if(currTimePeriod < expectedEndTime) {
		Sleep((expectedEndTime - currTimePeriod)*MotionPrimitiveTimePeriod());
	}
}

model fun StartExecutingPath(path: seq[int], startT: int, robotId: int)
{

}

model fun Sleep(time: int)
{

}

machine PlanExecutorMachine {
	var motionplanner: machine;
	var robotId: int;
	var localTimeV: machine;
	start state Init {
		entry(payload: (mp: machine, rid: int)) {
			motionplanner = payload.mp;
			robotId = payload.rid;
			send motionplanner, ePlanExecutorMachine, this;
			receive {
				case eTimeSyncId: (ts: machine) { localTimeV = ts; }
			}
			goto WaitForPathToExecute;
		}
	}

	state WaitForPathToExecute {
		on eStartExecutingPlan do (payload: TimedTrajType) {
			var currTimePeriod: int;
			var traj: seq[int];
			var index: int;

			index = 0;
			while(index < sizeof(payload))
			{
				traj += (index, payload[index].1);
			    index = index + 1;
			}

			InvokeStartExecutingPathAndWait(traj, payload[0].0, robotId, localTimeV);

			send motionplanner, ePlanCompletion, traj[sizeof(traj) - 1];
		}
	}
}

