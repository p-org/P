package psym.runtime.logger;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;
import java.math.BigDecimal;

public class BacktrackWriter {
  static PrintWriter log = null;

  public static void Initialize(String projectName, String outputFolder) {
    try {
      // get new file name
      String fileName = outputFolder + "/backtrack-" + projectName + ".log";
      // Define new file printer
      File statFile = new File(fileName);
      statFile.getParentFile().mkdirs();
      statFile.createNewFile();
      log = new PrintWriter(statFile);
    } catch (IOException e) {
      System.out.println("Failed to set printer to the StatLogger!!");
    }
  }

  public static void log(String name, BigDecimal prefixCoverage, int depth, int choiceDepth) {
    log.println(String.format("%s %.20f %d %d", name, prefixCoverage, depth, choiceDepth));
    log.flush();
  }
}
