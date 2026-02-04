package pobserve.report;

import pobserve.commandline.PObserveExitStatus;
import pobserve.commons.PObserveEvent;
import pobserve.commons.exceptions.PObserveEventOutOfOrderException;
import pobserve.commons.exceptions.PObserveLogParsingException;
import pobserve.executor.PObserveReplayEvents;
import pobserve.logger.PObserveLogger;
import pobserve.runtime.exceptions.PAssertionFailureException;

import java.io.BufferedWriter;
import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;
import java.nio.charset.StandardCharsets;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;

import static pobserve.config.PObserveConfig.getPObserveConfig;

/**
 * Used to track all errors from a PObserve job
 */
public class TrackErrors {
    private static final List<PObserveError> errors = new ArrayList<>();

    // Adds an error
    public static synchronized void addError(PObserveError error) {
        errors.add(error);
    }

    // Resets the tracksErrors array list
    public static synchronized void reset() {
        errors.clear();
    }

    // Checks if there are currently any errors
    public static boolean hasErrors() {
        return !errors.isEmpty();
    }

    // Generates a report log file of all current errors
    public static void emitErrorsReport() {
        if (hasErrors()) {
            int parserExceptionCount = 0;
            int unkownExceptionCount = 0;
            for (PObserveError error : errors) {
                PObserveLogger.info("-------------------------------------");
                if (error.getException() instanceof PAssertionFailureException) {
                    PObserveLogger.error("PObserve Spec Violation::\n" + error.getException().getMessage());
                    logReplayEventsToFile(error.getException(), error.getReplayEvents());
                } else if (error.getException() instanceof PObserveEventOutOfOrderException) {
                    PObserveLogger.error("PObserve Event Out of Order Exception::");
                    PObserveLogger.error(error.getException().getMessage());
                    logReplayEventsToFile(error.getException(), error.getReplayEvents());
                } else if (error.getException() instanceof PObserveLogParsingException) {
                    PObserveLogger.error("PObserve Parser Exception::");
                    PObserveLogger
                            .error("Exception occurred while parsing log line: " + error.getException().getMessage());
                    logParserExceptionToFile(error.getException(), parserExceptionCount);
                    parserExceptionCount++;
                } else {
                    PObserveLogger.error("Failed with an unknown exception:: " + error.getException().getMessage());
                    PObserveLogger.error("Please report this issue to the P team");
                    logUnknownExceptionToFile(error.getException(), unkownExceptionCount);
                    unkownExceptionCount++;
                }
                PObserveLogger.info("-------------------------------------");
            }
        } else {
            PObserveLogger.info("-------------------------------------");
            PObserveLogger.info("Success! No bugs found.");
            PObserveLogger.info("-------------------------------------");
        }
    }

    // Generates a file containing unknown exceptions
    private static void logUnknownExceptionToFile(Exception e, int count) {

        String outputFilePath = Paths
                .get(getPObserveConfig().getOutputDir().getAbsolutePath(), "UnknownError_" + count + ".txt").toString();

        PObserveLogger.info(".. writing error log into file " + outputFilePath);
        try {
            FileWriter fw = new FileWriter(outputFilePath, StandardCharsets.UTF_8);
            PrintWriter writer = new PrintWriter(fw);
            writer.write("errorMessage=");
            e.printStackTrace(writer);
            writer.write(String.format("%nerrorLogLine=%n%s", e.getMessage()));
            writer.close();
            fw.close();
        } catch (IOException ex) {
            throw new RuntimeException("Exception occurred while writing exception details to file", ex);
        }
        String message = "Exception details can be found in:\n\t" + outputFilePath;
        PObserveLogger.info(message);
    }

    // Generates a file containing parser exceptions
    private static void logParserExceptionToFile(Exception e, int count) {

        String outputFilePath = Paths
                .get(getPObserveConfig().getOutputDir().getAbsolutePath(), "ParserError_" + count + ".txt").toString();

        PObserveLogger.info(".. writing error log into file " + outputFilePath);
        try {
            FileWriter fw = new FileWriter(outputFilePath, StandardCharsets.UTF_8);
            PrintWriter writer = new PrintWriter(fw);
            writer.write("errorMessage=");
            e.printStackTrace(writer);
            writer.write(String.format("%nerrorLogLine=%n%s", e.getMessage()));
            writer.close();
            fw.close();
        } catch (IOException ex) {
            throw new RuntimeException("Exception occurred while writing parser exception to output file", ex);
        }
        String message = "Exception details can be found in:\n\t" + outputFilePath;
        PObserveLogger.info(message);
    }

    // Generates a file containing PObserve exceptions and their associated replay
    // window
    public static void logReplayEventsToFile(Exception exception, PObserveReplayEvents replay) {
        FileWriter fw = null;
        try {
            String sanitizedKey = replay.getKey().replaceAll("[^a-zA-Z0-9-_]", "_");
            int keyLength = sanitizedKey.length();

            // Shorten the key if it is too long to use as a filename
            String filename = keyLength > 200
                    ? sanitizedKey.substring(0, 100) + "__" + sanitizedKey.substring(keyLength - 100, keyLength)
                    : sanitizedKey;
            String replayLogFile = "replayEvents_" + filename + ".log";
            replayLogFile = Paths.get(getPObserveConfig().getOutputDir().getAbsolutePath(), replayLogFile).toString();
            fw = new FileWriter(replayLogFile, StandardCharsets.UTF_8);

            PObserveLogger.info(".. writing error log into file " + replayLogFile);

            BufferedWriter writer = new BufferedWriter(fw);
            writer.write(String.format("errorKey=%s%n", replay.getKey()));
            writer.write(String.format("errorTimeStamp=%d%n", replay.getErrorTimeStamp()));
            writer.write(String.format("errorMessage=%s%n", exception.getMessage()));
            writer.write("replayEvents=\n");
            while (!replay.getReplayEventQueue().isEmpty()) {
                PObserveEvent event = replay.getReplayEventQueue().poll();
                writer.write(event.toString());
                writer.newLine();
            }
            writer.close();
            fw.close();
        } catch (IOException e) {
            throw new RuntimeException(e);
        } finally {
            if (fw != null) {
                try {
                    fw.close();
                } catch (IOException ignored) {
                }
            }
        }
    }

    // Returns number of errors
    public static int numErrors() {
        return errors.size();
    }

    // Returns type of PObserveExitStatus
    public static PObserveExitStatus getExitStatus() {
        if (hasErrors()) {
            if (errors.stream().anyMatch(ex -> !((ex.getException() instanceof PAssertionFailureException)
                    || (ex.getException() instanceof PObserveLogParsingException)))) {
                return PObserveExitStatus.INTERNALERROR;
            } else if (errors.stream().anyMatch(ex -> (ex.getException() instanceof PAssertionFailureException))) {
                return PObserveExitStatus.PASSERT;
            } else {
                return PObserveExitStatus.PARSELOGERROR;
            }
        } else {
            return PObserveExitStatus.SUCCESS;
        }
    }
}
