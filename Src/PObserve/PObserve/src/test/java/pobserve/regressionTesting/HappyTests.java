package pobserve.regressionTesting;

import pobserve.metrics.EventMetrics;
import pobserve.report.TrackErrors;

import java.io.IOException;
import java.util.Iterator;
import java.util.Map;
import java.util.stream.Stream;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

import static pobserve.metrics.PObserveMetrics.getPObserveMetrics;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;

public class HappyTests extends TestEnvironment {
    @ParameterizedTest(name = "WHEN_TestCase{0}_WITH_Key{1}_LogLines{2}_{3}_THEN_HAPPY")
    @MethodSource("testData")
    void happyTests(String testCase, String key, String logLines, String sorted, Map<String, String> updates) throws Exception {

        HelperFunctions.runPObserveJob(updates);

        assertFalse(TrackErrors.hasErrors());

        Iterator<EventMetrics> it = getPObserveMetrics().getEventMetrics().values().iterator();
        int numVerifiedEvents = 0;
        while (it.hasNext()) {
            numVerifiedEvents += it.next().getVerified();
        }
        assertEquals(Integer.valueOf(logLines), numVerifiedEvents);
    }



    public static Stream<Arguments> testData() throws IOException {
        return HelperFunctions.generateTestData(PObserveArgsConstants.happyLogsPath);
    }
}
