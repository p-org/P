package psym.runtime.logger;

import lombok.Getter;
import psym.runtime.PSymGlobal;
import psym.runtime.machine.Machine;
import psym.runtime.machine.Monitor;
import psym.runtime.machine.State;
import psym.runtime.machine.events.Event;
import psym.runtime.machine.events.Message;
import psym.valuesummary.GuardedValue;
import psym.valuesummary.PrimitiveVS;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;
import java.util.List;

public class ScheduleWriter {
    static PrintWriter log = null;
    @Getter static String fileName = "";
    private static int logIdx = 0;

    public static void Initialize(String projectName, String outputFolder) {
        try {
            // get new file name
            fileName = outputFolder + "/" + projectName + ".schedule";
            // Define new file printer
            File schFile = new File(fileName);
            schFile.getParentFile().mkdirs();
            schFile.createNewFile();
            log = new PrintWriter(schFile);
        } catch (IOException e) {
            System.out.println("Failed to set printer to the ScheduleWriter!!");
        }
    }

    private static void log(String value) {
        log.println(value);
        log.flush();
    }

    private static void logComment(String message) {
        log(String.format("// Step %d: %s", logIdx, message));
        logIdx++;
    }

    public static void logBoolean(PrimitiveVS<Boolean> res) {
        List<GuardedValue<Boolean>> gv = res.getGuardedValues();
        assert (gv.size() == 1);
        logComment("boolean choice");
        log(gv.get(0).getValue() ? "True" : "False");
    }

    public static void logInteger(PrimitiveVS<Integer> res) {
        List<GuardedValue<Integer>> gv = res.getGuardedValues();
        assert (gv.size() == 1);
        logComment("integer choice");
        log(gv.get(0).getValue().toString());
    }

    public static void logReceive(Machine target, State state, Event event) {
        if (!(target instanceof Monitor)) {
            if (!state.isIgnored(event) && !state.isDeferred(event)) {
                logComment(String.format("receive %s at %s in state %s",
                        event,
                        target,
                        state));
                log(target.toString());
            }
        }
    }

    public static void logSend(Machine sender, Message msg) {
        List<GuardedValue<Event>> eventGv = msg.getEvent().getGuardedValues();
        assert (eventGv.size() == 1);
        Event event = eventGv.get(0).getValue();

        List<GuardedValue<Machine>> targetGv = msg.getTarget().getGuardedValues();
        assert (targetGv.size() == 1);
        Machine target = targetGv.get(0).getValue();

        List<GuardedValue<State>> senderStateGv = sender.getCurrentState().getGuardedValues();
        assert (senderStateGv.size() == 1);
        State senderState = senderStateGv.get(0).getValue();

        List<GuardedValue<State>> targetStateGv = target.getCurrentState().getGuardedValues();
        assert (targetStateGv.size() == 1);
        State targetState = targetStateGv.get(0).getValue();

        if (!(sender instanceof Monitor)) {
            logComment(String.format("send %s from %s in state %s to %s in state %s",
                    event,
                    sender,
                    senderState,
                    target,
                    targetState));
            log(sender.toString());
        }
    }

    public static void logUnblock(Machine target, Message msg) {
        List<GuardedValue<Event>> eventGv = msg.getEvent().getGuardedValues();
        assert (eventGv.size() == 1);
        Event event = eventGv.get(0).getValue();

        List<GuardedValue<State>> targetStateGv = target.getCurrentState().getGuardedValues();
        assert (targetStateGv.size() == 1);
        State targetState = targetStateGv.get(0).getValue();

        assert (!(target instanceof Monitor));

        logComment(String.format("unblocked %s in state %s on receiving %s",
                target,
                targetState,
                event));
        log(target.toString());
    }

    public static void logHeader() {
        if (!PSymGlobal.getConfiguration().getTestDriver().equals(PSymGlobal.getConfiguration().getTestDriverDefault())) {
            log(String.format("--test-method:%s", PSymGlobal.getConfiguration().getTestDriver()));
        }
        logComment("create GodMachine");
        log("Task(0)");
        logComment("start GodMachine");
        log("Plang.CSharpRuntime._GodMachine(1)");
        logComment("create Main(2)");
        log("Plang.CSharpRuntime._GodMachine(1)");
    }
}
