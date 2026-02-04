# PObserve CLI

The PObserve CLI tool enables you to verify formal specifications against service or test logs on your local machine. It's ideal for processing smaller sets of logs (a few GBs).

## Getting Started

**[Step 1] Building PObserve**

To set up and build PObserve, please refer to the [Setup and Build PObserve](./setuppobservecli.md) guide.

**[Step 2] Running PObserve CLI**

Once you have built the PObserve JAR locally, use the following command in the console, adjusting the options as needed for your specific use case:

```bash
java -jar <path_to_pobserve_jar> \
  --jars <path_to_spec_and_parser_jar> \
  --spec <spec_name> \
  --parser <parser_name> \
  -l <path_to_log_file> \
  --logDelimiter <log_delimiter> \
  --outputDir <path_to_output_directory> \
  --inputKind <input_type> \
  --replay <replay_window_size>
```

## Command-Line Options

| Parameter           | Required | Description                                                                         |
|---------------------|----------|-------------------------------------------------------------------------------------|
| `--help`, `-h`      | No       | Lists all supported options and commands                                            |
| `--jars`, `-j`      | Yes      | Path to JARs containing monitor and parser suppliers (multiple JAR paths must be space separated) |
| `--spec`            | Yes      | Name of the P specification to be checked on the logs                               |
| `--logs`, `-l`      | Yes      | Log file to read or directory with multiple log files                               |
| `--parser`, `-p`    | No       | Parser supplier class name (fully qualified). Required only if multiple parsers exist in the JAR |
| `--logDelimiter`    | No       | Delimiter used to parse the log file (end of line delimiter)                        |
| `--outputDir`, `-o` | No       | Path to output directory. Default is a new folder in the current working directory  |
| `--replay`          | No       | Replay window size. Default is 100                                                  |
| `--inputKind`, `-ik`| No       | Input kind (supported formats: `text` or `json`)                                    |

## PObserve CLI Output (Results)

PObserve CLI run will generate several files in the output directory based on the execution results:

1. **Metrics Log**: File containing the metrics of the PObserve CLI run such as total events verified, number of errors, execution time, etc.
2. **Parser Error Log**: Contains parser exception message and the specific log line that caused the parser exception
3. **PObserve Error Log**: Contains specification or unexpected exception messages along with a log of replay events for debugging purposes
