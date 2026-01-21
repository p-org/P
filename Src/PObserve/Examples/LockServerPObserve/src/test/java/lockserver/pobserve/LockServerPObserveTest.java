package lockserver.pobserve;

import pobserve.junit.PObserveLogFileBaseTest;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertDoesNotThrow;
import java.io.File;

import lockserver.pobserve.spec.PMachines;
import lockserver.pobserve.parser.LockServerParser;
import pobserve.junit.PObserveJUnitSpecConfig;
import pobserve.runtime.exceptions.PAssertionFailureException;
import java.io.BufferedReader;
import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStream;

@PObserveJUnitSpecConfig(
        parser = LockServerParser.class,
        monitors = {
                PMachines.MutualExclusion.Supplier.class,
                PMachines.ResponseOnlyOnRequest.Supplier.class
        }
)
public class LockServerPObserveTest extends PObserveLogFileBaseTest {

    @Test
    public void testErrorCaseLogFileWith1Key() {
        InputStream inputStream = getResourceFromFile("lock_server_log_10000_1_error.txt");
        Exception e = assertThrows(PAssertionFailureException.class, () -> runPObserveOnLogFile(inputStream));
        String expectedErrorMsg = "Spec Error: PSpec/LockServerCorrect.p:44:9 Lock 0 is already acquired, expects lock error but received lock success";
        if (e != null) {
            assertTrue(e.getMessage().contains(expectedErrorMsg));
        }
    }

    @Test
    public void testErrorCaseLogFileWith5Keys() {
        InputStream inputStream = getResourceFromFile("lock_server_log_10000_5_error.txt");
        Exception e = assertThrows(PAssertionFailureException.class, () -> runPObserveOnLogFile(inputStream));
        String expectedErrorMsg = "Spec Error: PSpec/LockServerCorrect.p:44:9 Lock 2 is already acquired, expects lock error but received lock success.";
        if (e != null) {
            assertTrue(e.getMessage().contains(expectedErrorMsg));
        }
    }

    @Test
    public void testHappyCaseLogFileWith1Key() {
        InputStream inputStream = getResourceFromFile("lock_server_log_10000_1_happy.txt");
        assertDoesNotThrow(() -> runPObserveOnLogFile(inputStream));
    }

    @Test
    public void testHappyCaseLogFileWith5Keys() {
        InputStream inputStream = getResourceFromFile("lock_server_log_10000_5_happy.txt");
        assertDoesNotThrow(() -> runPObserveOnLogFile(inputStream));
    }

    private InputStream getResourceFromFile(String file) {
        return LockServerPObserveTest.class.getClassLoader().getResourceAsStream(file);
    }
}