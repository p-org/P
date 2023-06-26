package psym.runtime.logger;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;
import psym.runtime.statistics.CoverageStats;

public class CoverageWriter {
  static PrintWriter log = null;
  static boolean enabled = true;

  public static void Initialize(String projectName, String outputFolder) {
    try {
      // get new file name
      String fileName = outputFolder + "/coverage-" + projectName + ".log";
      // Define new file printer
      File statFile = new File(fileName);
      statFile.getParentFile().mkdirs();
      statFile.createNewFile();
      log = new PrintWriter(statFile);
    } catch (IOException e) {
      System.out.println("Failed to add appender to the CoverageLogger!!");
    }
  }

  public static void info(String msg) {
    if (enabled) {
      log.println(msg);
      log.flush();
    }
  }

  public static void log(int step, CoverageStats.CoverageDepthStats val) {
    info(
        String.format(
            "%15s%15s%15s%15s%15s",
            step,
            val.getNumScheduleExplored(),
            val.getNumDataExplored(),
            val.getNumScheduleRemaining(),
            val.getNumDataRemaining()));
  }

  public static void enable() {
    enabled = true;
  }

  public static void disable() {
    enabled = false;
  }
}
