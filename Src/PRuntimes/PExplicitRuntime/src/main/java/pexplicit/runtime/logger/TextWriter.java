package pexplicit.runtime.logger;

import lombok.Getter;
import pexplicit.runtime.PExplicitGlobal;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;

public class TextWriter {
    private static final int logIdx = 0;
    static PrintWriter log = null;
    @Getter
    static String fileName = "";

    public static void Initialize() {
        try {
            // get new file name
            fileName = PExplicitGlobal.getConfig().getOutputFolder() + "/" + PExplicitGlobal.getConfig().getProjectName() + "_0_0.txt";
            // Define new file printer
            File schFile = new File(fileName);
            schFile.getParentFile().mkdirs();
            schFile.createNewFile();
            log = new PrintWriter(schFile);
        } catch (IOException e) {
            System.out.println("Failed to set printer to the TextWriter.");
        }
    }

    private static void log(String value) {
        log.println(value);
        log.flush();
    }

    public static void typedLog(LogType type, String message) {
        log(String.format("<%s> %s", type, message));
    }
}
