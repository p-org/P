package testcases.failuredetector;

import java.util.LinkedHashSet;
import java.util.Set;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

import static testcases.failuredetector.FailureDetector.*;

public class FailureDetectorTest {
    @Test
    @DisplayName("Can notify nodes down")
    public void testCanNotifyNodesDown() {
        ReliableFailureDetector m  = new ReliableFailureDetector();
        m.ready();

        assertEquals(0, m.get_nodesDownDetected().size());
        assertEquals(0, m.get_nodesShutdownAndNotDetected().size());
        m.accept(new eShutDown(1L));
        assertEquals(0, m.get_nodesDownDetected().size());
        assertEquals(1, m.get_nodesShutdownAndNotDetected().size());

        LinkedHashSet<Long> nodes = new LinkedHashSet<>(Set.of(1L, 2L, 3L));
        m.accept(new eNotifyNodesDown(nodes));
        assertEquals(3, m.get_nodesDownDetected().size());
        assertEquals(0, m.get_nodesShutdownAndNotDetected().size());
    }
}
