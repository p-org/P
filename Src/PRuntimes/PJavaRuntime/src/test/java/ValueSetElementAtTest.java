import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import prt.values.SetIndexing;

import java.util.*;
import java.util.stream.Collectors;
import java.util.stream.IntStream;

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
    @DisplayName("Returns valid elements on sequential accesses")
    public void testValidSequentialIndexing() {
        LinkedHashSet<Integer> s =
                new LinkedHashSet<>(IntStream.range(1, SetIndexing.MIN_SETSIZE + 1)
                        .boxed()
                        .collect(Collectors.toList()));

        for (int i = 0; i < s.size(); i++) {
            assertEquals(prt.values.SetIndexing.elementAt(s, i), i+1);
        }

        for (int i = s.size() - 1; i >= 0; i--) {
            assertEquals(prt.values.SetIndexing.elementAt(s, i), i+1);
        }
    }

    @Test
    @DisplayName("Returns valid elements on random accesses")
    public void testValidRandomIndexing() {
        LinkedHashSet<Integer> s =
                new LinkedHashSet<>(IntStream.range(1, SetIndexing.MIN_SETSIZE + 1)
                        .boxed()
                        .collect(Collectors.toList()));

        // Arbitrary iteration
        Random r = new Random(System.currentTimeMillis());
        for (int i = 0; i < 500; i++) {
            int idx = r.nextInt(s.size());
            assertEquals(prt.values.SetIndexing.elementAt(s, idx), idx+1);

            // Periodically force the iterator cache to be invalidated in a few different ways:

            // 1) Perform a minor collection, which will zap out unretained weak references.
            if (r.nextInt(10) == 0) {
                System.gc();
            }

            // 2) Construct a new set, so the cached set will differ by pointer comparison to the current one.
            if (r.nextInt(10) == 0) {
                s = new LinkedHashSet<>(IntStream.range(1, SetIndexing.MIN_SETSIZE + 1)
                                .boxed()
                                .collect(Collectors.toList()));
                if (r.nextInt(5) == 0) {
                    System.gc();
                }
            }

            // 3) Mutate the collection, which will internally result in an ConcurrentModificationException to be thrown
            // when using the now-invalid cached iterator.
            if (r.nextInt(10) == 0) {
                s.add(s.size() + 1);
            }
        }
    }
}
