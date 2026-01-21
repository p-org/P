import pobserve.commons.utils.LogBreaker;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;

public class LogBreakerTests {
    private String singleLineLog =
            "line 1\n" +
            "line 2\n" +
            "line 3\n";

    private String multiLineLog =
            "line 1\n" +
            "another line 1\n" +
            "EOE\n" +
            "line 2\n" +
            "another line 2\n" +
            "EOE\n" +
            "line 3\n" +
            "another line 3\n" +
            "EOE\n";

    @Test
    public void testSingleLineLog() throws IOException {
        test("\n", singleLineLog, 7);
    }

    @Test
    public void testMultiLineLog() throws IOException {
        test("\nEOE\n", multiLineLog, 26);
    }

    private void test(String delimiter, String log, long stride) throws IOException {
        // This test assumes that all of the log entries are the same length
        // The "stride" is the number of bytes that should be consumed by the next log line
        for (long skip = 0; skip < log.length(); skip += stride) {
            InputStream stringReader = new ByteArrayInputStream(log.getBytes(StandardCharsets.UTF_8));
            stringReader.skip(skip);

            LogBreaker logBreaker = new LogBreaker(delimiter, stringReader, skip);

            while (logBreaker.hasNext()) {
                String line = logBreaker.next();
                long index = logBreaker.getByteCount();
                Assertions.assertEquals(0, index % stride);
            }
        }
    }
}
