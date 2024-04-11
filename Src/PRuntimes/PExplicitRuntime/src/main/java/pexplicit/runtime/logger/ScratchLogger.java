package pexplicit.runtime.logger;

import lombok.Setter;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.core.Appender;
import org.apache.logging.log4j.core.LoggerContext;
import org.apache.logging.log4j.core.appender.OutputStreamAppender;
import org.apache.logging.log4j.core.layout.PatternLayout;
import pexplicit.runtime.PExplicitGlobal;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.util.Date;

/**
 * Represents the scratch logger
 */
public class ScratchLogger {
    static Logger log = null;
    static LoggerContext context = null;

    @Setter
    static int verbosity;

    public static void Initialize() {
        verbosity = PExplicitGlobal.getConfig().getVerbosity();
        log = Log4JConfig.getContext().getLogger(ScratchLogger.class.getName());
        org.apache.logging.log4j.core.Logger coreLogger =
                (org.apache.logging.log4j.core.Logger) LogManager.getLogger(ScratchLogger.class.getName());
        context = coreLogger.getContext();

        try {
            // get new file name
            Date date = new Date();
            String fileName = PExplicitGlobal.getConfig().getOutputFolder() + "/scratch" + ".log";
            File file = new File(fileName);
            file.getParentFile().mkdirs();
            file.createNewFile();

            // Define new file printer
            FileOutputStream fout = new FileOutputStream(fileName, false);

            PatternLayout layout = Log4JConfig.getPatternLayout();
            Appender fileAppender =
                    OutputStreamAppender.createAppender(layout, null, fout, fileName, false, true);
            fileAppender.start();

            context.getConfiguration().addLoggerAppender(coreLogger, fileAppender);
        } catch (IOException e) {
            System.out.println("Failed to set printer to the ScratchLogger!!");
        }
    }

    public static void log(String str) {
        log.info(str);
    }
}
