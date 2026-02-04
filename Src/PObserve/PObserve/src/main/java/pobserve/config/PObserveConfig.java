package pobserve.config;

import pobserve.commons.PObserveEventFilter;
import pobserve.commons.Parser;
import pobserve.commons.config.CreateInstanceFromJar;
import pobserve.commons.config.InstanceType;
import pobserve.runtime.events.PEvent;
import pobserve.utils.DateTimeUtils;

import com.beust.jcommander.ParameterException;
import edu.umd.cs.findbugs.annotations.SuppressFBWarnings;
import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.function.Consumer;
import java.util.function.Supplier;
import lombok.Getter;
import lombok.Setter;

/**
 * This class implements the PObserve configuration.
 */
@Setter
@Getter
public class PObserveConfig {

  //region constants
  /**
   * Default port number for socket mode
   */
  public static final int DEFAULT_PORT = 9876;

  //region instance
  private static final PObserveConfig instance;
  private PObserveConfig() {
    outputDir = new File("PObserve-" + DateTimeUtils.getCurrentDateTime()); // default
    inputKind = SourceInputKind.TEXT; //default
    supplierJars = new ArrayList<>();
    parserName = null;
    specificationName = null;
    filterNames = new ArrayList<>();
    filterSuppliers = new ArrayList<>();
    replayWindowSize = 100; // default
    assumeInputFilesAreSorted = false;
    keys = new HashSet<>();
    socketMode = false;
    port = DEFAULT_PORT;
    host = "localhost";
  }

  static {
    try {
      instance = new PObserveConfig();
    } catch (Exception e) {
      throw new RuntimeException("Exception occurred in creating PObserveConfig instance", e);
    }
  }

  @SuppressFBWarnings("MS_EXPOSE_REP")
  public static synchronized PObserveConfig getPObserveConfig() {
    return instance;
  }

  public String toString() {
    String config = "PObserveConfig {\n" +
        String.format("  %-20s = %s%n", "outputDir",  outputDir) +
        String.format("  %-20s = %s%n", "inputKind", inputKind) +
        String.format("  %-20s = %s%n", "supplierJars", supplierJars) +
        String.format("  %-20s = %s%n", "parserName",  parserName) +
        String.format("  %-20s = %s%n", "specificationName", specificationName) +
        String.format("  %-20s = %d%n", "replayWindowSize", replayWindowSize);

    if (parserSupplier != null) {
        config += String.format("  %-20s = %s%n", "logDelimiter", parserSupplier.getLogDelimiter().replace("\n", "\\n"));
    }

    if (socketMode) {
        config += String.format("  %-20s = %s%n", "socketMode", socketMode) +
                  String.format("  %-20s = %s%n", "host", host) +
                  String.format("  %-20s = %s%n", "port", port);
    }

    config += '}';
    return config;
  }
  //endregion

  //region fields

  /**
   * Class Name of the P Specification to check
   */
  private String specificationName;

  /**
   * The Supplier that implements the P specification to be checked
   */
  private Supplier<Consumer<PEvent<?>>> specificationSupplier;
  /**
   * The Supplier that implements the Parser to be used for parsing the logs and generating PObserveEvents
   */
  private Parser<?> parserSupplier;
  /**
   * Class Name of the parser to be used for parsing the logs and generating PObserveEvents
   */
  private String parserName;

  /**
   * Optional string to be passed to the parser instance through setConfiguration()
   */
  private String parserConfiguration;

  /**
   * The Supplier that implements the PObserveEventFilter to be used for filtering the parsed PObserveEvents
   */
  private List<PObserveEventFilter<?>> filterSuppliers;
  /**
   * Class Names of pobserve event filters to be used for filtering the parsed PObserveEvents
   */
  private List<String> filterNames;
  /**
   * Output directory to store the results.
   */
  private File outputDir;
  /**
   * The list of log files to be processed for checking the specification
   */
  private List<File> logFiles;

  /**
   * Input files kind: {TEXT or JSON}
   */
  private SourceInputKind inputKind;
  /**
   * Input jar files that have P specification and Log parser classes
   */
  private List<String> supplierJars;
  /**
   * Size of replay window
   */
  private int replayWindowSize;

  /**
   * Assume that the input files are sorted based on time
   */
  private boolean assumeInputFilesAreSorted;

  /**
   * Keys to filter events
   */
  private HashSet<String> keys;

  /**
   * Socket mode enabled flag
   */
  private boolean socketMode;

  /**
   * Port to listen on in socket mode
   */
  private int port;

  /**
   * Host to bind to in socket mode
   */
  private String host;


  public static void validateAndLoadPObserveConfig() throws IOException {
    if (getPObserveConfig().getSupplierJars().isEmpty()) {
      throw new ParameterException("No supplierJars specified. Please provide jar file that has the log parser and the consumer supplier.");
    }

    // Validate socket mode parameters
    if (getPObserveConfig().isSocketMode()) {
      // In socket mode, log files are optional
      if (getPObserveConfig().getPort() <= 0 || getPObserveConfig().getPort() > 65535) {
        throw new ParameterException("Invalid port number. Port must be between 1 and 65535.");
      }
      if (getPObserveConfig().getHost() == null || getPObserveConfig().getHost().isEmpty()) {
        throw new ParameterException("Host cannot be empty in socket mode.");
      }
    } else {
      // In non-socket mode, log files are required
      if (getPObserveConfig().getLogFiles() == null || getPObserveConfig().getLogFiles().isEmpty()) {
        throw new ParameterException("No log files specified. Please provide log file or directory with --logs parameter.");
      }
    }

    CreateInstanceFromJar createInstanceFromJar = new CreateInstanceFromJar(getPObserveConfig().getSupplierJars(), PObserveConfig.class.getClassLoader());
    getPObserveConfig().setParserSupplier((Parser<?>) createInstanceFromJar.getInstance(InstanceType.PARSER, getPObserveConfig().getParserName()));
    String parserConfiguration = getPObserveConfig().getParserConfiguration();
    if (parserConfiguration != null) {
        getPObserveConfig().getParserSupplier().setConfiguration(parserConfiguration);
    }
    getPObserveConfig().setSpecificationSupplier((Supplier<Consumer<PEvent<?>>>) createInstanceFromJar.getInstance(InstanceType.SPECIFICATION_SUPPLIER, getPObserveConfig().getSpecificationName()));
    for (String filterName: getPObserveConfig().getFilterNames()) {
      getPObserveConfig().getFilterSuppliers().add((PObserveEventFilter<?>) createInstanceFromJar.getInstance(InstanceType.FILTER, filterName));
    }

    // create output directory if not exists
    Files.createDirectories(Paths.get(getPObserveConfig().getOutputDir().getAbsolutePath()));

    // validation succeeded. Print the current configuration of the PObserve run.
    System.out.println("Current PObserve configuration:");
    System.out.println(getPObserveConfig());
  }
}
