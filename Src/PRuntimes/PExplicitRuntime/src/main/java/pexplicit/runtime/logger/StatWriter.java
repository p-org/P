package pexplicit.runtime.logger;

import pexplicit.runtime.PExplicitGlobal;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;

public class StatWriter {
    static PrintWriter log = null;

    public static void Initialize() {
        try {
            // get new file name
            String fileName = PExplicitGlobal.getConfig().getOutputFolder() + "/stats-" + PExplicitGlobal.getConfig().getProjectName() + ".log";
            // Define new file printer
            File statFile = new File(fileName);
            statFile.getParentFile().mkdirs();
            statFile.createNewFile();
            log = new PrintWriter(statFile);
        } catch (IOException e) {
            System.out.println("Failed to set printer to the StatLogger!!");
        }
    }

    public static void log(String key, String value) {
        log.println(String.format("%-40s%s", key + ":", value));
        log.flush();
    }
}