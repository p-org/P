package symbolicp.runtime;

import symbolicp.bdd.Bdd;
import symbolicp.vs.*;

import java.util.List;
import java.util.logging.Level;
import java.util.logging.Logger;

public class ScheduleLogger {

    private final static Logger log = LoggingUtils.getLog("SCHEDULE");

    /* If turned on, logs the path constraints and goto/raise outcomes */
    private static boolean isVerbose = false;

    public static void onProcessEvent(Bdd pc, Machine machine, Event event)
    {
        String msg = String.format("Machine %s is processing event %s in state %s", machine, event, machine.getState().guard(pc));
        if (isVerbose) msg = String.format("under path %s ", pc) + msg;
        log.fine(msg);
    }

    public static void onProcessStateTransition(Bdd pc, Machine machine, PrimVS<State> newState) {
        String msg = String.format("Machine %s transitioning to state %s", machine.toString(), newState);
        if (isVerbose) msg = String.format("under path %s ", pc) + msg;
        log.info(msg);
    }

    public static void onCreateMachine(Bdd pc, Machine machine) {
        String msg = "Machine " + machine + " was created";
        log.info(msg);
    }

    public static void onMachineStart(Bdd pc, Machine machine) {
        String msg = String.format("Machine %s starting", machine.toString());
        if (isVerbose) msg = String.format("under path %s ", pc) + msg;
        log.info(msg);
    }

    public static void machineState(Bdd pc, Machine machine) {
        String msg = String.format("Machine %s in state %s", machine, machine.getState().guard(pc));
        log.info(msg);
    }
    /*
    public static void log(Object ... message) {
        base.info("<PrintLog> " + String.join(", ", Arrays.toString(message)));
    }
     */
    public static void setVerbose(boolean verbose) {
        isVerbose = verbose;
    }

    public static void finished(int steps) {
        log.info(String.format("Execution finished in %d steps", steps));
    }

    public static void handle(Machine m, State st, Event event) {
        log.info("Machine " + m + " handling event " + event + " in state " + st);
    }

    public static void disable() {
        log.setLevel(Level.OFF);
    }

    public static void enable() { log.setLevel(Level.ALL); }

    public static void send(Event effect) {
        String msg = "Send effect " + effect + " to " + effect.getMachine();
        log.info(msg);
    }

    public static void schedule(int step, Event effect) {
        String msg = "Step " + step + ": scheduled " + effect + " sent to " + effect.getMachine();
        log.info(msg);
    }

    public static void push(PrimVS<State> state) {
        log.info("Pushing state " + state + " onto stack");
    }

    public static void log(String str) {
        log.info(str);
    }
}
