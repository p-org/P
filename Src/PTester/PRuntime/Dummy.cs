public class Application: PStateImpl {
    public Application() {

    }

    List<Main> Main_list;
    
    public IEnumerable<Machine> AllMachines {
        get {
	    foreach (var m in Main_list) {
	        yield return m;
	    }
	    yield break;
	}
    }

    public IEnumerable<Monitor> AllMonitors {
        get {
	    yield break;
	}
    }

    public class Main: Machine {
        public Main(int instance): base(instance, 0, -1) {
	    
	}

	public AnonFun0_Fun: Fun<Main> {
	    public void Execute() {
	       ...
	    }

	    public void PushFrame() {
	       ...
	    }
	}

	public static AnonFun0_Fun AnonFun0 = new AnonFun0_Fun();
	
	public static State Init = new State(...);

	
	static {
	    
	}
    }

    public static Event NullEvent = new Event(...);
    public static Event HaltEvent = new Event(...);
}