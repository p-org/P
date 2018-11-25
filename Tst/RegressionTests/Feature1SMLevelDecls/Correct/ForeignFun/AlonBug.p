// This sample XYZs case when exit actions are not executed
type T;
event E;

machine Main {
	var t: T;
	var i: int;
	start state Init {
		entry {
			ForeignFun();
			i = 0;
			raise E;
		}
		//this assert is unreachable:
		//after the Call state is popped with (i == 3), the queue is empty,
		// machine keeps waiting for an event, and exit actions are never executed
		exit {
			assert(false);
		}
		on E push Call;
	}

	state Call {
		entry {
			GlobalForeignFun();
			if (i == 3) {
				pop;
			} else {
				i = i + 1;
			}
			raise E; //Call is popped
		}
	}

	fun ForeignFun();
}

fun GlobalForeignFun();