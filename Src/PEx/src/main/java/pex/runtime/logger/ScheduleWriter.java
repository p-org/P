package pex.runtime.logger;

import lombok.Getter;
import pex.runtime.PExGlobal;
import pex.runtime.machine.PMachine;
import pex.utils.misc.Assert;
import pex.values.PBool;
import pex.values.PInt;
import pex.values.PValue;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;

public class ScheduleWriter {
    static PrintWriter log = null;
    @Getter
    static String fileName = "";
    private static int logIdx = 0;

    public static void Initialize() {
        try {
            // get new file name
            fileName = PExGlobal.getConfig().getOutputFolder() + "/" + PExGlobal.getConfig().getProjectName() + "_0_0.steps";
            // Define new file printer
            File schFile = new File(fileName);
            schFile.getParentFile().mkdirs();
            schFile.createNewFile();
            log = new PrintWriter(schFile);
        } catch (IOException e) {
            System.out.println("Failed to set printer to the ScheduleWriter.");
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

    public static void logDataChoice(PValue<?> choice) {
        Class type = choice.getClass();
        if (choice instanceof PBool boolChoice) {
            logComment("boolean choice");
            log(boolChoice.getValue() ? "True" : "False");
        } else if (choice instanceof PInt intChoice) {
            logComment("integer choice");
            log(intChoice.toString());
        } else {
            assert false;
        }
    }

    public static void logScheduleChoice(PMachine choice) {
        logComment("schedule choice");
        log(String.format("(%d)", choice.getInstanceId()));
    }

    public static void logHeader() {
        if (!PExGlobal.getConfig().getTestDriver().equals(PExGlobal.getConfig().getTestDriverDefault())) {
            log(String.format("--test-method:%s", PExGlobal.getConfig().getTestDriver()));
        }
        if (Assert.getFailureType().equals("cycle")) {
            log(String.format("--cycle-detected: %s", Assert.getFailureMsg()));
        }
        logComment("create GodMachine");
        log("(0)");
        logComment("start GodMachine");
        log("(1)");
        logComment("create Main(2)");
        log("(1)");
    }
}
