package pobserve.source.file;

import pobserve.commons.utils.LogBreaker;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.util.stream.Stream;

import static pobserve.config.PObserveConfig.getPObserveConfig;

/**
 * TextLogFileReader class helps read logs from a text file
 */
public class TextLogFileReader implements PObserveFileReader {

    /**
     * Reads logs from a text file
     * @param file text file containing logs
     * @return stream of log lines
     */
    @Override
    public Stream<Object> readFile(File file) {
        LogBreaker lb;
        Stream.Builder<Object> streamBuilder = Stream.builder();
        try (FileInputStream fileInputStream = new FileInputStream(file)) {
            lb = new LogBreaker(getPObserveConfig().getParserSupplier().getLogDelimiter(), fileInputStream);
            while (lb.hasNext()) {
                streamBuilder.accept(lb.next().trim());
            }
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
        try {
            return streamBuilder.build();
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }
}
