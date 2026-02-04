package pobserve.source.file;

import pobserve.config.SourceInputKind;

import java.io.File;
import java.util.stream.Stream;

import static pobserve.config.PObserveConfig.getPObserveConfig;

/**
 * PObserveFileReader class helps read log files
 */
public interface PObserveFileReader {
    Stream<Object> readFile(File file);

    public static PObserveFileReader getFileReader() {
        if (getPObserveConfig().getInputKind() == SourceInputKind.TEXT) {
            return new TextLogFileReader();
        } else {
            return new PTraceJSONFileReader();
        }
    }
}
