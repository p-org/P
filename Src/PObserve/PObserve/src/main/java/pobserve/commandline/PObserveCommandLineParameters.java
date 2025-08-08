package pobserve.commandline;

import pobserve.commandline.paramvalidator.ValidateFilterParam;
import pobserve.commandline.paramvalidator.ValidateHost;
import pobserve.commandline.paramvalidator.ValidateInputKind;
import pobserve.commandline.paramvalidator.ValidateInputsAreSorted;
import pobserve.commandline.paramvalidator.ValidateJars;
import pobserve.commandline.paramvalidator.ValidateKeyListFile;
import pobserve.commandline.paramvalidator.ValidateKeys;
import pobserve.commandline.paramvalidator.ValidateLogLocation;
import pobserve.commandline.paramvalidator.ValidateOutputDir;
import pobserve.commandline.paramvalidator.ValidateParserConfigurationParam;
import pobserve.commandline.paramvalidator.ValidateParserParam;
import pobserve.commandline.paramvalidator.ValidatePort;
import pobserve.commandline.paramvalidator.ValidateReplayWindow;
import pobserve.commandline.paramvalidator.ValidateSocketMode;
import pobserve.commandline.paramvalidator.ValidateSpecName;
import pobserve.config.PObserveConfig;
import pobserve.config.SourceInputKind;

import com.beust.jcommander.Parameter;
import com.beust.jcommander.Parameters;
import java.util.List;
import lombok.Getter;
import lombok.ToString;

@Getter
@ToString
@Parameters
public class PObserveCommandLineParameters {

  @Parameter(
      names = {"--help", "-h"},
      description = "Lists all supported options and commands",
      help = true)
  private boolean help; //NOPMD

  @Parameter(
          names = {"--spec"},
          description = "Name of the P specification to be checked on the logs",
          arity = 1,
          required = true,
          validateWith = ValidateSpecName.class)
  private String specName;

  @Parameter(
      names = {"--jars", "-j"},
      description = "jars with consumer and parser suppliers",
      required = true,
      validateWith = ValidateJars.class,
      variableArity = true)
  private List<String> jars;

  @Parameter(
      names = {"--parser", "-p"},
      description = "parser supplier, exact class name",
      arity = 1,
      validateWith = ValidateParserParam.class)
  private String parser;

  @Parameter(
      names = {"--parserConfiguration", "-pc"},
      description = "string to be passed to parser",
      required = false,
      validateWith = ValidateParserConfigurationParam.class)
  private String parserConfiguration;

  @Parameter(
        names = {"--filters", "-f"},
        description = "pobserve event filter exact class names",
        variableArity = true,
        validateWith = ValidateFilterParam.class)
  private String filters;

  @Parameter(
          names = {"--logs", "-l"},
          description = "log file to read or directory with multiple log files",
          arity = 1,
          required = false,
          validateWith = ValidateLogLocation.class)
  private String file;

  @Parameter(
          names = {"--socket-mode"},
          description = "Enable socket command API mode",
          arity = 0,
          validateWith = ValidateSocketMode.class)
  private boolean socketMode;

  @Parameter(
          names = {"--port"},
          description = "Port to listen on in socket mode",
          arity = 1,
          validateWith = ValidatePort.class)
  private int port = PObserveConfig.DEFAULT_PORT;

  @Parameter(
          names = {"--host"},
          description = "Host to bind to in socket mode (default: localhost)",
          arity = 1,
          validateWith = ValidateHost.class)
  private String host = "localhost";

  @Parameter(
          names = {"--inputkind", "-ik"},
          description = "Input log kind {TEXT or JSON}",
          arity = 1,
          validateWith = ValidateInputKind.class)
  private SourceInputKind inputKind;

  @Parameter(
          names = {"--outputDir", "-o"},
          description = "Output directory",
          arity = 1,
          validateWith = ValidateOutputDir.class)
  private String outputBucket;

  @Parameter(
          names = {"--replaySize"},
          description = "Replay window size",
          arity = 1,
          validateWith = ValidateReplayWindow.class)
  private String replayWindow;

  @Parameter(
          names = {"--assumeInputsAreSorted"},
          description = "Assume that the input files are already sorted",
          validateWith = ValidateInputsAreSorted.class)
  private boolean assumeInputsAreSorted;

  @Parameter(
          names = {"--keys"},
          description = "Keys to be filtered and verified",
          validateWith = ValidateKeys.class,
          variableArity = true)
  private List<String> keys;

  @Parameter(
          names = {"--keyListFile"},
          description = "Path to file containing list of keys(one key per line) to be filtered and verified",
          validateWith = ValidateKeyListFile.class)
  private String keyListFile;
}
