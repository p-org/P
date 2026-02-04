package pobserve.junit;

import java.io.InputStream;
import java.io.IOException;
import java.util.List;
import java.util.function.Supplier;
import java.util.stream.Stream;

import org.junit.jupiter.api.BeforeEach;

import pobserve.commons.Parser;
import pobserve.commons.utils.LogBreaker;
import pobserve.junit.utils.ParserAndMonitorProvider;
import pobserve.runtime.events.PEvent;

public class PObserveLogFileBaseTest {
    private PObserveLogAppender pobserveLogAppender;
    private String logDelimiter;

    /**
     * Sends read log lines to pobserveLogAppender to run monitor
     */
    public void runPObserveOnLogFile(InputStream inputStream) throws Exception {
        Stream<String> loglines = readLogsFromFile(inputStream);
        loglines.forEachOrdered(line -> pobserveLogAppender.append(line));
        pobserveLogAppender.close();
    }

    /**
     * Gets spec configs from annotation, makes a new pobserveLogAppender using TotalEventSequencer
     */
    @BeforeEach
    public void setup() throws Exception {
        PObserveJUnitSpecConfig annotation = this.getClass().getAnnotation(PObserveJUnitSpecConfig.class);
        Parser<PEvent<?>> parser = ParserAndMonitorProvider.getParser(annotation.parser());
        List<Supplier<?>> monitorSuppliers = ParserAndMonitorProvider.getMonitorSuppliers(annotation.monitors());

        TotalEventSequencer totalEventSequencer = new TotalEventSequencer(monitorSuppliers);
        pobserveLogAppender = new PObserveLogAppender(parser, totalEventSequencer);
        logDelimiter = parser.getLogDelimiter();
    }

    /**
     * Reads log lines from log file
     */
    private Stream<String> readLogsFromFile(InputStream inputStream) throws IOException {
        LogBreaker lb;
        Stream.Builder<String> streamBuilder = Stream.builder();

        lb = new LogBreaker(logDelimiter, inputStream);
        while (lb.hasNext()) {
            streamBuilder.accept(lb.next().trim());
        }
        try {
            return streamBuilder.build();
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }
}
