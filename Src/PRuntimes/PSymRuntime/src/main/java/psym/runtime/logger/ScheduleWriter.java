package psym.runtime.logger;

import lombok.Getter;
import psym.commandline.PSymConfiguration;
import psym.runtime.machine.Machine;
import psym.runtime.machine.events.Event;
import psym.runtime.machine.events.Message;
import psym.valuesummary.GuardedValue;
import psym.valuesummary.PrimitiveVS;
import psym.valuesummary.ValueSummary;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;
import java.util.List;

public class ScheduleWriter {
    static PrintWriter log = null;
    @Getter static String fileName = "";

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

    public static void logBoolean(PrimitiveVS<Boolean> res) {
        List<GuardedValue<Boolean>> gv = res.getGuardedValues();
        assert (gv.size() == 1);
        log("// boolean choice");
        log(gv.get(0).getValue() ? "True" : "False");
    }

    public static void logInteger(PrimitiveVS<Integer> res) {
        List<GuardedValue<Integer>> gv = res.getGuardedValues();
        assert (gv.size() == 1);
        log("// integer choice");
        log(gv.get(0).getValue().toString());
    }

    public static void logElement(ValueSummary res) {
        List<GuardedValue<?>> gv = ValueSummary.getGuardedValues(res);
        assert (gv.size() == 1);
        log("// element choice");
        log(gv.get(0).getValue().toString());
    }

    public static void logSend(Machine sender, Message msg) {
        List<GuardedValue<Event>> eventGv = msg.getEvent().getGuardedValues();
        assert (eventGv.size() == 1);
        List<GuardedValue<Machine>> targetGv = msg.getTarget().getGuardedValues();
        assert (targetGv.size() == 1);
        log(String.format("// send %s from %s to %s", eventGv.get(0).getValue(), sender, targetGv.get(0).getValue()));
        log(sender.toString());
    }

    public static void logReceive(Message msg) {
        List<GuardedValue<Event>> eventGv = msg.getEvent().getGuardedValues();
        assert (eventGv.size() == 1);
        List<GuardedValue<Machine>> targetGv = msg.getTarget().getGuardedValues();
        assert (targetGv.size() == 1);
        log(String.format("// receive %s at %s", eventGv.get(0).getValue(), targetGv.get(0).getValue()));
        log(targetGv.get(0).getValue().toString());
    }

    public static void logHeader(PSymConfiguration config) {
        if (!config.getTestDriver().equals(config.getTestDriverDefault())) {
            log(String.format("--test-method:%s", config.getTestDriver()));
        }
        log("// create GodMachine");
        log("Task(0)");
        log("// start GodMachine");
        log("Plang.CSharpRuntime._GodMachine(1)");
        log("// create Main(2)");
        log("Plang.CSharpRuntime._GodMachine(1)");
    }
}
