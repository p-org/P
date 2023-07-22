package psym.runtime.logger;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;
import java.util.List;
import lombok.Getter;
import psym.runtime.PSymGlobal;
import psym.runtime.machine.Machine;
import psym.runtime.machine.Monitor;
import psym.runtime.machine.State;
import psym.runtime.machine.events.Event;
import psym.runtime.machine.events.Message;
import psym.valuesummary.GuardedValue;
import psym.valuesummary.PrimitiveVS;
import psym.valuesummary.UnionVS;

public class TextWriter {
    static PrintWriter log = null;
    @Getter static String fileName = "";

    public static void Initialize() {
        try {
            // get new file name
            fileName = PSymGlobal.getConfiguration().getOutputFolder() + "/" + PSymGlobal.getConfiguration().getProjectName() + "_0_0.txt";
            // Define new file printer
            File schFile = new File(fileName);
            schFile.getParentFile().mkdirs();
            schFile.createNewFile();
            log = new PrintWriter(schFile);
        } catch (IOException e) {
            System.out.println("Failed to set printer to the TextWriter!!");
        }
    }

    private static void log(String value) {
        log.println(value);
        log.flush();
    }

    private static void logLine(LogType type, String message) {
        log(String.format("<%s> %s", type.toString(), message));
    }

    public static void logDequeue(Machine target, State state, Event event, UnionVS payload) {
        if (!event.equals(Event.createMachine)) {
            if (payload == null || payload.isEmptyVS()) {
                logLine(LogType.DequeueLog,
                        String.format("'%s' dequeued event '%s' in state '%s'.",
                                target,
                                event,
                                state));
            } else {
                logLine(LogType.DequeueLog,
                        String.format("'%s' dequeued event '%s with payload %s' in state '%s'.",
                                target,
                                event,
                                payload,
                                state));
            }
        }
    }

    public static void logEnqueue(Machine sender, Message msg) {
        List<GuardedValue<Event>> eventGv = msg.getEvent().getGuardedValues();
        assert (eventGv.size() == 1);
        Event event = eventGv.get(0).getValue();

        if (event.equals(Event.createMachine)) {
            logCreate(sender, msg);
            return;
        }

        List<GuardedValue<Machine>> targetGv = msg.getTarget().getGuardedValues();
        assert (targetGv.size() == 1);
        Machine target = targetGv.get(0).getValue();

        List<GuardedValue<State>> senderStateGv = sender.getCurrentState().getGuardedValues();
        assert (senderStateGv.size() == 1);
        State senderState = senderStateGv.get(0).getValue();

        UnionVS payload = msg.getPayload();

        if (!(sender instanceof Monitor)) {
            if (payload == null || payload.isEmptyVS()) {
                logLine(
                    LogType.SendLog,
                    String.format(
                        "'%s' in state '%s' sent event '%s' to '%s'.",
                        sender, senderState, event, target));
            } else {
                logLine(
                        LogType.SendLog,
                        String.format(
                                "'%s' in state '%s' sent event '%s with payload %s' to '%s'.",
                                sender, senderState, event, payload, target));
            }
        }
    }

    public static void logCreate(Machine sender, Message msg) {
        List<GuardedValue<Machine>> targetGv = msg.getTarget().getGuardedValues();
        assert (targetGv.size() == 1);
        Machine target = targetGv.get(0).getValue();

        assert (!(sender instanceof Monitor));
        if (target.getInstanceId() != Machine.getMainMachineId()) {
            logLine(LogType.CreateLog, String.format("%s was created by %s.", target, sender));
        }
    }


    public static void logStateEntry(Machine machine, State state) {
        logLine(LogType.StateLog,
                String.format("%s enters state '%s'.",
                        machine,
                        state));
    }

    public static void logStateExit(Machine machine, State state) {
        logLine(LogType.StateLog,
                String.format("%s exits state '%s'.",
                        machine,
                        state));
    }

    public static void logGoto(Machine machine, PrimitiveVS<State> gotoState) {
        List<GuardedValue<State>> currStateGv = machine.getCurrentState().getGuardedValues();
        assert (currStateGv.size() == 1);
        State currState = currStateGv.get(0).getValue();

        List<GuardedValue<State>> nextStateGv = gotoState.getGuardedValues();
        assert (nextStateGv.size() == 1);
        State nextState = nextStateGv.get(0).getValue();

        logLine(LogType.GotoLog,
                String.format("%s is transitioning from state '%s' to state '%s'.",
                        machine,
                        currState,
                        nextState));
    }

    public static void logUnblock(Machine target, Message msg) {
        List<GuardedValue<Event>> eventGv = msg.getEvent().getGuardedValues();
        assert (eventGv.size() == 1);
        Event event = eventGv.get(0).getValue();

        List<GuardedValue<State>> targetStateGv = target.getCurrentState().getGuardedValues();
        assert (targetStateGv.size() == 1);
        State targetState = targetStateGv.get(0).getValue();

        assert (!(target instanceof Monitor));

        logLine(LogType.ReceiveLog,
                String.format("unblocked %s in state %s on receiving %s",
                target,
                targetState,
                event));
    }

    public static void logBug(String msg) {
        logLine(LogType.ErrorLog, msg);
    }

    public static void logHeader() {
        logLine(LogType.TestLog, String.format("Running test '%s'.", PSymGlobal.getConfiguration().getTestDriver()));
    }
}
