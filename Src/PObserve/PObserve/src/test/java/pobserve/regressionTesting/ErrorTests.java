package pobserve.regressionTesting;

import pobserve.commandline.PObserveExitStatus;
import pobserve.report.TrackErrors;

import java.io.IOException;
import java.util.AbstractMap;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.stream.Stream;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertSame;
import static org.junit.jupiter.api.Assertions.assertTrue;


public class ErrorTests extends TestEnvironment {
    @ParameterizedTest(name = "WHEN_TestCase{0}_WITH_Key{1}_LogLines{2}_{3}_THEN_ERROR")
    @MethodSource("testData")
    void errorTests(String testCase, String key, String logLines, String sorted, Map<String, String> updates) throws Exception {
        // Get error details created from pobserve job
        AbstractMap.SimpleEntry<List<Long>, List<String>> errorDetails = HelperFunctions.runPObserveJob(updates);
        List<Long> errorTimestamps = errorDetails.getKey();
        List<String> errorMessages = errorDetails.getValue();

        // Get expected error details, trim message to match PObserveLocal message format
        String path = String.format("unit-test-logs/error-messages/test_case_%s_logs/test_case_%s_log_%s_%s_error.txt", testCase, testCase, logLines, key);
        AbstractMap.SimpleEntry<List<Long>, List<String>> expectedErrorDetails = HelperFunctions.getExpectedErrors(path);
        List<Long> expectedErrorTimestamps = expectedErrorDetails.getKey();

        List<String> tempExpectedErrorMessages = expectedErrorDetails.getValue();
        String expectedErrorMessagePrefix = "Spec Violation: Consumer threw an exception: ";

        List<String> expectedErrorMessages = new ArrayList<String>();
        for (String message: tempExpectedErrorMessages) {
            String expectedErrorMessage = HelperFunctions.getEndString(message, expectedErrorMessagePrefix);
            expectedErrorMessages.add(expectedErrorMessage.substring(0, expectedErrorMessage.length() - 1));
        }

        // Assertions
        assertTrue(TrackErrors.hasErrors());
        assertEquals(expectedErrorTimestamps.size() , TrackErrors.numErrors());

        // assert that errors are found and they are all assertion violation
        assertSame(PObserveExitStatus.PASSERT, TrackErrors.getExitStatus());

        assertEquals(expectedErrorTimestamps.size(), errorTimestamps.size());
        assertEquals(expectedErrorMessages.size(), errorMessages.size());

        for (Long errorTimestamp : errorTimestamps) {
            assertTrue(expectedErrorTimestamps.contains(errorTimestamp), "Timestamp: " + errorTimestamp + " was not in the expected timestamp values: " + expectedErrorTimestamps.toString());
        }
        for (String errorMessage : errorMessages) {
            assertTrue(expectedErrorMessages.contains(errorMessage), "Message: " + errorMessage + " was not in the expected error messages: " + expectedErrorMessages.toString());
        }
    }

    public static Stream<Arguments> testData() throws IOException {
        return HelperFunctions.generateTestData(PObserveArgsConstants.errorLogsPath);
    }
}
