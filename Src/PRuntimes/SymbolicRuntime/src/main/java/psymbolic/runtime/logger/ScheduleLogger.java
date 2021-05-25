package psymbolic.runtime.logger;

import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.Message;
import psymbolic.runtime.machine.State;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.PrimitiveVS;

public class ScheduleLogger extends PLogger {

    /* If turned on, logs the path constraints and goto/raise outcomes */
    private static boolean isVerbose = false;

    public static void onProcessEvent(Guard pc, Machine machine, Message message)
    {
        String msg = String.format("Machine %s is processing event %s in state %s", machine, message.getEvent(), machine.getState().restrict(pc));
        if (isVerbose) msg = String.format("under path %s ", pc) + msg;
        log.info(msg);
    }

    public static void onProcessStateTransition(Guard pc, Machine machine, PrimitiveVS<State> newState) {
        String msg = String.format("Machine %s transitioning to state %s", machine.toString(), newState);
        if (isVerbose) msg = String.format("under path %s ", pc) + msg;
        log.info(msg);
    }

    public static void onCreateMachine(Guard pc, Machine machine) {
        String msg = "Machine " + machine + " was created";
        log.info(msg);
    }

    public static void onMachineStart(Guard pc, Machine machine) {
        String msg = String.format("Machine %s starting", machine.toString());
        if (isVerbose) msg = String.format("under path %s ", pc) + msg;
        log.info(msg);
    }

    public static void machineState(Guard pc, Machine machine) {
        String msg = String.format("Machine %s in state %s", machine, machine.getState().restrict(pc));
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

    public static void handle(Machine m, State st, Message event) {
        log.info("Machine " + m + " handling event " + event.getEvent() + " in state " + st);
    }

    public static void send(Message effect) {
        String msg = "Send effect " + effect.getEvent() + " to " + effect.getTarget();
        log.info(msg);
    }

    public static void schedule(int step, Message effect) {
        String msg = "Step " + step + ": scheduled " + effect.toString() + " sent to " + effect.getTarget();
        log.info(msg);
    }

    public static void push(PrimitiveVS<State> state) {
        log.info("Pushing state " + state + " onto stack");
    }

    public static void log(String str) {
        log.info(str);
    }
}
