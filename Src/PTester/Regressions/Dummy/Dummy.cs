//Manually generated new compiler output for a dummy P program:
//machine Main {
//	start state Init {
//	}
//}
public class Application : PStateImpl {
    //similar field for each Machine and Monitor:
    List<Main> MainMachines;

    //TODO: implemented differently in PingPong.cs
    public override IEnumerable<BaseMachine> AllAliveMachines {
        get {
            //generate such loop for each Machine:
            foreach (var m in MainMachines) {
                yield return m;
            }
            yield break;
        }
    }
    //TODO: implemented differently in PingPong.cs
    public override IEnumerable<BaseMonitor> AllInstalledMonitors {
        get {
            //similar (as in AllMachines) foreach loop for each Monitor
            yield break;
        }
    }
    #region Constructors
    public Application() : base()
    {
        //initialize all fields
    }
    public Application(bool initialize) : base(initialize)
    {
        //initialize all fields
    }
    #endregion
    //similar class Machine for each Machine and
    //class Monitor for each Monitor
    public class Main : Machine<Main>
    {
        public Main(int instance) : base(instance, 0, -1)
        {

        }
        //similar class for each function local in Main machine:
        internal class AnonFun0_Fun : Fun<Main>
        {
            //no body for now:
            public void Execute() { }
            //no body for now:
            public void PushFrame() { }
        }
        //declare similar static field for each function local in Main machine:
        public static AnonFun0_Fun AnonFun0 = new AnonFun0_Fun();
        //declare similar static field for each state of the Main machine:
        public static State Init = new State();
    }
    //declare static class (similar to AnonFun0_Fun) and  
    //static field for each static function:
    static class StaticFun: StaticFun<Main> {
        //no body for now:
        public void Execute() { }
        //no body for now:
        public void PushFrame() { }
    }
    //declare static field for each event:
    public static Event NullEvent = new Event();
    public static Event HaltEvent = new Event();
}