import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.util.LinkedHashSet;
import java.util.List;
import java.util.NoSuchElementException;
import java.util.Random;

import static org.junit.jupiter.api.Assertions.*;

/** Tests `Values.setElementAt()`. */
public class ValueSetElementAtTest {

    @Test
    @DisplayName("Throws on negative index")
    public void testNegativeIndex() {
        LinkedHashSet<Integer> s = new LinkedHashSet<>(List.of(1,2,3,4,5));
        assertThrows(NoSuchElementException.class, () -> prt.values.SetIndexing.elementAt(s, -1));
    }

    @Test
    @DisplayName("Throws on out-of-bounds index")
    public void testOOB() {
        LinkedHashSet<Integer> s = new LinkedHashSet<>(List.of(1,2,3,4,5));
        assertThrows(NoSuchElementException.class, () -> prt.values.SetIndexing.elementAt(s, 5));
    }

    @Test
    @DisplayName("Throws on an empty set")
    public void testEmptySet() {
        LinkedHashSet<Integer> s = new LinkedHashSet<>();
        assertThrows(NoSuchElementException.class, () -> prt.values.SetIndexing.elementAt(s, 0));
    }

    @Test
    @DisplayName("Returns valid elements on valid indices")
    public void testValidIndexing() {
        LinkedHashSet<Integer> s = new LinkedHashSet<>(List.of(1,2,3,4,5));

        // In-order iteration
        assertEquals(prt.values.SetIndexing.elementAt(s, 0), 1);
        assertEquals(prt.values.SetIndexing.elementAt(s, 1), 2);
        assertEquals(prt.values.SetIndexing.elementAt(s, 2), 3);
        assertEquals(prt.values.SetIndexing.elementAt(s, 3), 4);
        assertEquals(prt.values.SetIndexing.elementAt(s, 4), 5);

        // Arbitrary iteration
        Random r = new Random(42);
        for (int i = 0; i < 100; i++) {
            int idx = r.nextInt(5);
            assertEquals(prt.values.SetIndexing.elementAt(s, idx), idx+1);
        }
    }
}
