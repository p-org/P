import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import prt.exceptions.IncomparableValuesException;
import prt.values.*;
import testcases.clientserver.PEvents;
import testcases.clientserver.PTypes;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import static org.junit.jupiter.api.Assertions.*;

/**
 * Validates Value.compare() and Value.equals().
 */
public class ValueCompareTest {

    @Test
    @DisplayName("compare() is well-defined for nulls")
    public void testNullComparisons() {
        Integer i = 42;
        assertEquals(Equality.compare(null, i), -1);
        assertEquals(Equality.compare(null, null), 0);
        assertEquals(Equality.compare(i, null), 1);
    }

    @Test
    @DisplayName("compare() boxed primitive types")
    public void testPrimitiveComparisons() {
        assertEquals(Equality.compare(999, 42), 1);
        assertEquals(Equality.compare(42, 42), 0);
        assertEquals(Equality.compare(49, 999), -1);

        assertEquals(Equality.compare(999L, 42L), 1);
        assertEquals(Equality.compare(42L, 42L), 0);
        assertEquals(Equality.compare(49L, 999L), -1);

        assertEquals(Equality.compare(3.14, 2.71), 1);
        assertEquals(Equality.compare(2.71, 2.71), 0);
        assertEquals(Equality.compare(2.71, 3.14), -1);

        assertEquals(Equality.compare("za", "az"), 1);
        assertEquals(Equality.compare("a", "a"), 0);
        assertEquals(Equality.compare("az", "za"), -1);
    }

    enum anEnum {
        VALUE_ZERO(0),
        VALUE_ONE(1);
        private final int value;
        anEnum(int i) { value = i; }
    }

    enum anotherEnum {
        VALUE_0(0),
        VALUE_1(1);
        private final int value;
        anotherEnum(int i) { value = i; }
    }

    @Test
    @DisplayName("tests enums")
    public void testEnumEquality()
    {
        assertEquals(anEnum.VALUE_ZERO, anEnum.VALUE_ZERO);
        assertNotEquals(anEnum.VALUE_ZERO, anEnum.VALUE_ONE);
        assertEquals(anEnum.VALUE_ONE, anEnum.VALUE_ONE);

        assertEquals(Equality.compare(anEnum.VALUE_ZERO, anEnum.VALUE_ZERO), 0);
        assertEquals(Equality.compare(anEnum.VALUE_ZERO, anEnum.VALUE_ONE), -1);
        assertEquals(Equality.compare(anEnum.VALUE_ONE, anEnum.VALUE_ZERO), 1);
        assertEquals(Equality.compare(anEnum.VALUE_ONE, anEnum.VALUE_ONE), 0);

        assertTrue(Equality.deepEquals(anEnum.VALUE_ZERO, anEnum.VALUE_ZERO));
        assertFalse(Equality.deepEquals(anEnum.VALUE_ZERO, anEnum.VALUE_ONE));

        assertThrows(IncomparableValuesException.class, () ->
                Equality.deepEquals(anEnum.VALUE_ZERO, anotherEnum.VALUE_0));
    }

    @Test
    @DisplayName("equals() is well-defined for nulls")
    public void testNullEquality() {
        Object o = new Object();
        assertFalse(Equality.deepEquals(o, null));
        assertTrue(Equality.deepEquals(null, null));
        assertFalse(Equality.deepEquals(null, o));
    }

    @Test
    @DisplayName("equals() does not coerse numeric types")
    public void testNoCorersionForEquality() {
        // int <-> bool
        assertThrows(IncomparableValuesException.class, () -> Equality.deepEquals(0, false));
        assertThrows(IncomparableValuesException.class, () -> Equality.deepEquals(1, true));

        // int <-> float
        assertThrows(IncomparableValuesException.class, () -> Equality.deepEquals(0, 0.0f));
        assertThrows(IncomparableValuesException.class, () -> Equality.deepEquals(1, 1.0f));

        // int <-> long
        assertThrows(IncomparableValuesException.class, () -> Equality.deepEquals(0, 0L));
        assertThrows(IncomparableValuesException.class, () -> Equality.deepEquals(1, 1L));
    }

    @Test
    @DisplayName("equals() correctly does deep equality checks")
    public void testDeepEquality() {
        HashMap<String, ArrayList<Integer>> m1 = new HashMap<>(Map.of("123", new ArrayList<>(List.of(1, 2, 3))));
        HashMap<String, ArrayList<Integer>> m2 = new HashMap<>(Map.of("123", new ArrayList<>(List.of(1, 2, 3))));

        assertFalse(m1.get("123") == m2.get("123")); // Ensure that the values are different references
        assertTrue(Equality.deepEquals(m1, m2));
    }
    @Test
    @DisplayName("hashcode() and equals() is correctly overridden for P tuples and events.")
    public void testPTypeHashing() {
        // Tuples.
        PTypes.PTuple_src_accnt_amnt_rId t1 = new PTypes.PTuple_src_accnt_amnt_rId();
        PTypes.PTuple_src_accnt_amnt_rId t2 = new PTypes.PTuple_src_accnt_amnt_rId();

        assertFalse(t1 == t2);
        assertTrue(t1.equals(t2));
        assertTrue(Equality.deepEquals(t1, t2));
        assertTrue(t1.hashCode() == t2.hashCode());

        // Events.
        PEvents.eWithDrawReq e1 = new PEvents.eWithDrawReq(t1);
        PEvents.eWithDrawReq e2 = new PEvents.eWithDrawReq(t1);

        assertFalse(e1 == e2);
        assertTrue(e1.equals(e2));
        assertTrue(Equality.deepEquals(e1, e2));
        assertTrue(e1.hashCode() == e2.hashCode());
    }
}
