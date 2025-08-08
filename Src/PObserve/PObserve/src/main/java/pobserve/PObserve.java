package pobserve;

import pobserve.commandline.PObserveCommandLineParameters;
import pobserve.commandline.PObserveExitStatus;
import pobserve.config.PObserveConfig;
import pobserve.executor.PObserveExecutor;
import pobserve.logger.PObserveLogger;
import pobserve.metrics.MetricConstants;
import pobserve.report.PObserveError;
import pobserve.report.TrackErrors;
import pobserve.source.socket.PObserveSocketServer;

import com.beust.jcommander.JCommander;
import com.beust.jcommander.ParameterException;

import static pobserve.config.PObserveConfig.getPObserveConfig;
import static pobserve.metrics.PObserveMetrics.getPObserveMetrics;

public final class PObserve {
  /**
   * Main function of the program. It parses program parameters, instantiates a Sequencer, and
   * executes it.
   *
   * @param args is arguments of PObserve.
   * @throws Exception if given arguments are not well-formed.
   */
  public static void main(String[] args) throws Exception {
    PObserveCommandLineParameters params = new PObserveCommandLineParameters();
    JCommander jc = JCommander.newBuilder().addObject(params).build();

    // parse the commandline arguments and load the config
    try {
      jc.parse(args);

      // Check if help was requested
      if (params.isHelp()) {
        jc.usage();
        return;
      }

      PObserveConfig.validateAndLoadPObserveConfig();
    } catch (ParameterException e) {
      PObserveLogger.error("Failed parsing ::");
      PObserveLogger.error(e.getMessage());
      jc.usage();
      System.exit(PObserveExitStatus.CMDLINEERROR.getValue());
    }

    // Check if we should run in socket mode
    if (getPObserveConfig().isSocketMode()) {
      runSocketMode();
    } else {
      runStandardMode();
    }
  }

  /**
   * Run PObserve in standard file processing mode.
   */
  private static void runStandardMode() {
    try {
      var job = new PObserveExecutor();
      job.run();
    } catch (Exception ex) {
      TrackErrors.addError(new PObserveError(ex));
    } finally {
      TrackErrors.emitErrorsReport();
      getPObserveMetrics().outputMetricsSummary();
      printFinalMessage();
      System.exit(TrackErrors.getExitStatus().getValue());
    }
  }

  /**
   * Run PObserve in socket server mode.
   */
  private static void runSocketMode() {
    PObserveLogger.info("Starting PObserve in socket mode");
    PObserveSocketServer server = new PObserveSocketServer(
        getPObserveConfig().getHost(),
        getPObserveConfig().getPort()
    );

    try {
      // Register shutdown hook to stop the server gracefully on JVM exit
      Runtime.getRuntime().addShutdownHook(new Thread(() -> {
        PObserveLogger.info("Shutting down PObserve socket server");
        server.stop();
      }));

      // Start the server (this call blocks until the server is stopped)
      server.start();

    } catch (Exception ex) {
      PObserveLogger.error("Error in socket server mode: " + ex.getMessage());
      TrackErrors.addError(new PObserveError(ex));
      TrackErrors.emitErrorsReport();
      System.exit(PObserveExitStatus.INTERNALERROR.getValue());
    }
  }

  private static void printFinalMessage() {
    int errorCount = getPObserveMetrics().getMetricsMap().get(MetricConstants.TOTAL_SPEC_ERRORS).get()
            + getPObserveMetrics().getMetricsMap().get(MetricConstants.TOTAL_EVENT_OUT_OF_ORDER_ERRORS).get()
            + getPObserveMetrics().getMetricsMap().get(MetricConstants.TOTAL_UNKNOWN_ERRORS).get();
    assert errorCount == TrackErrors.numErrors();
    if (errorCount > 0) {
      PObserveLogger.error("PObserve run completed with " + errorCount + " error(s)");
      PObserveLogger.error("Run details and replay event logs can be found in " + getPObserveConfig().getOutputDir().getAbsolutePath());
    } else {
      PObserveLogger.info("PObserve run completed with " + errorCount + " error(s)");
      PObserveLogger.info("Run details can be found in " + getPObserveConfig().getOutputDir().getAbsolutePath());
    }
  }
}
