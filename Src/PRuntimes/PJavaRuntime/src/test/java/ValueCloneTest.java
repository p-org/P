import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import prt.exceptions.UncloneableValueException;


import java.util.*;
import java.util.concurrent.atomic.AtomicLong;

import static org.junit.jupiter.api.Assertions.*;
import static prt.values.Clone.deepClone;

public class ValueCloneTest {

    @Test
    @DisplayName("Can 'clone' a null value")
    public void testNullClone() {
        assertEquals(deepClone(null), null);
    }

    @Test
    @DisplayName("Can clone boxed primitive types")
    public void testClonePrimitives() {
        Boolean b = Boolean.valueOf(true);
        assertEquals(deepClone(b), b);

        Long i = Long.valueOf(31337);
        assertEquals(deepClone(i), i);

        Float f = Float.valueOf(1.61803f);
        assertEquals(deepClone(f), f);

        Long l = Long.valueOf(314159265L);
        assertEquals(deepClone(l), l);
    }

    @Test
    @DisplayName("Can clone lists")
    public void testCloneList() {
        // Ensure the clone completes successfully
        ArrayList<Long> a1 = new ArrayList<>(List.of(1L,2L,3L,4L,5L));
        ArrayList<Long> a2 = (ArrayList<Long>) deepClone(a1);
        assertEquals(a1, a2);

        // Now reassign an element and ensure only structural equality
        a1.set(1, 42L);
        assertNotEquals(a1, a2);
        a2.set(1, 42L);
        assertEquals(a1, a2);

        // Now append an element and ensure only structural equality.
        a1.add(99L);
        assertNotEquals(a1, a2);
        a2.add(99L);
        assertEquals(a1, a2);
    }

    @Test
    @DisplayName("Can clone sets")
    public void testCloneSet() {
        // Ensure the clone completes successfully
        LinkedHashSet<Long> s1 = new LinkedHashSet<>(List.of(1L,2L,3L,4L,5L));
        LinkedHashSet<Long> s2 = (LinkedHashSet<Long>) deepClone(s1);
        assertEquals(s1, s2);

        // Now mutate an element and ensure only structural equality
        s1.add(6L);
        assertNotEquals(s1, s2);
        s2.add(6L);
        assertEquals(s1, s2);
    }

    @Test
    @DisplayName("Can clone maps")
    public void testCloneMap() {
        HashMap<String, Long> m1 = new HashMap<>(Map.of(
                "A", 1L,
                "B", 2L,
                "C", 3L));
        HashMap<String, Long> m2 = (HashMap<String, Long>) deepClone(m1);
        assertEquals(m1, m2);


        // Ensure structural equality under adding elements
        m1.put("Z", 42L);
        assertNotEquals(m1, m2);
        m2.put("Z", 42L);
        assertEquals(m1, m2);

        // Ensure structural equality under removing elements
        m1.remove("B");
        assertNotEquals(m1, m2);
        m2.remove("B");
        assertEquals(m1, m2);

        // Ensure structural equality under reassigning values
        m1.put("C", 99L);
        assertNotEquals(m1, m2);
        m2.put("C", 99L);
        assertEquals(m1, m2);
    }

    @Test
    @DisplayName("Can clone nested collections")
    public void testNestedCollections() {
        HashMap<String, ArrayList<Long>> m1 = new HashMap<>(Map.of(
                "123", new ArrayList<>(List.of(1L,2L,3L)),
                "987", new ArrayList<>(List.of(9L,8L,7L))
        ));
        HashMap<String, ArrayList<Long>> m2 = (HashMap<String, ArrayList<Long>>) deepClone(m1);
        assertEquals(m1, m2);

        // Mutate a mutable reference value and ensure no aliasing
        m1.get("123").add(4L);
        assertNotEquals(m1, m2);
    }


    public static class PTuple_a implements prt.values.PValue<PTuple_a> {
        public ArrayList<Long> a;

        public PTuple_a() {
            this.a = new ArrayList<Long>();
        }

        public PTuple_a(ArrayList<Long> a) {
            this.a = a;
        }

        public PTuple_a deepClone() {
            return new PTuple_a((ArrayList<Long>)prt.values.Clone.deepClone(a));
        } // deepClone()

        public boolean equals(Object other) {
            return (this.getClass() == other.getClass() &&
                    this.deepEquals((PTuple_a)other)
            );
        } // equals()

        public boolean deepEquals(PTuple_a other) {
            return (true
                    && prt.values.Equality.deepEquals(this.a, other.a)
            );
        } // deepEquals()

        public String toString() {
            StringBuilder sb = new StringBuilder("PTuple_a");
            sb.append("[");
            sb.append("a=" + a);
            sb.append("]");
            return sb.toString();
        } // toString()
    } //PTuple_a class definition

    @Test
    @DisplayName("Can clone a tuple")
    public void testPtupleClone() {
        PTuple_a t1 = new PTuple_a(new ArrayList<>(List.of(1L,2L,3L)));
        PTuple_a t2 = (PTuple_a) deepClone(t1);

        assertTrue(t1.deepEquals(t2));

        t1.a.add(99L);
        assertFalse(t1.deepEquals(t2));
        assertNotEquals(t1.a, t2.a);
    }

    @Test
    @DisplayName("Cannot clone a non-P value class")
    public void testInvalidCloneOfUnrelatedClass() {
        // AtomicLong extends j.l.Number extends j.l.Object - this is a totally
        // distinct class from any P value the Java code generator emits.
        AtomicLong i = new AtomicLong(42);
        assertThrows(UncloneableValueException.class, () -> deepClone(i));
    }

    private class FancyLinkedHashSet<E> extends LinkedHashSet<E> {
        public FancyLinkedHashSet(List<E> e) { super(e); }
    }

    @Test
    @DisplayName("Cannot clone a subclass of a valid cloneable class")
    public void testInvalidCloneOfSubclass() {
        // LinkedHashSet extends hashSet, but we expect this should fail nonetheless.
        // (Relaxing this criterion would require walking the inheritance tree via
        // reflection, and it isn't clear what the return type of the cloned value would be.)
        FancyLinkedHashSet<Long> lh = new FancyLinkedHashSet<>(List.of(1L,2L,3L,4L,5L));
        assertThrows(UncloneableValueException.class, () -> deepClone(lh));
    }

    enum anEnum {
        VALUE_ZERO(0),
        VALUE_ONE(1);
        private final int value;
        anEnum(int i) { value = i; }
    }

    @Test
    @DisplayName("Can clone an enum")
    public void testEnumClone() {
        anEnum e1 = anEnum.VALUE_ONE;
        anEnum e2 = (anEnum) deepClone(e1);
        assertEquals(e1, e2);
    }


}
