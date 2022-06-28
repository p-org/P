package prt.values;

import java.util.*;

/**
 * Holds some routines for value cloning.
 */
public class Clone {

    /* Note: In theory, the Java compiler should be able to elide many of
     * the boxed primitive type calls (since Java knows it can unbox it and copy the
     * POD value to another variable), but for nested data structure cloning
     * they are included.
     */

    private static Boolean cloneBoolean(Boolean b) {
        return b; //already immutable!  No cloning necessary.
    }

    private static Integer cloneInteger(Integer i) {
        return i; //already immutable!  No cloning necessary.
    }

    private static Long cloneLong(Long l) {
        // NB: Actor IDs are stored as Longs.
        return l; //already immutable!  No cloning necessary.
    }

    private static Float cloneFloat(Float f) {
        return f; //already immutable!  No cloning necessary.
    }

    private static String cloneString(String s) {
        return s; //already immutable!  No cloning necessary.
    }

    private static Enum cloneEnum(Enum e) {
        return e; //already immutable!  No cloning necessary.
    }

    private static ArrayList<Object> cloneList(ArrayList<?> a) {
        ArrayList<Object> cloned = new ArrayList<>();
        cloned.ensureCapacity(a.size());
        for (Object val : a) {
            cloned.add(deepClone(val));
        }
        return cloned;
    }

    private static LinkedHashSet<Object> cloneSet(LinkedHashSet<?> s)
    {
        LinkedHashSet<Object> cloned = new LinkedHashSet<>();
        for (Object val : s) {
            cloned.add(deepClone(val));
        }
        return cloned;
    }

    private static HashMap<Object, Object> cloneMap(HashMap<?, ?> m)
    {
        HashMap<Object, Object> cloned = new HashMap<>();
        for (Map.Entry<?,?> e : m.entrySet()) {
            Object k = deepClone(e.getKey());
            Object v = deepClone(e.getValue());
            cloned.put(k, v);
        }
        return cloned;
    }

    /**
     * Performs a deep copy of a P program value by dispatching on the Object type.
     * (The performance difference between a hand-rolled deep copy and using serializers
     * appears to be significant[1], so doing the former seems to be a good idea.)
     * [1]: https://www.infoworld.com/article/2077578/java-tip-76--an-alternative-to-the-deep-copy-technique.html
     *
     * @param o the value to clone
     * @return a structurally-equivalent version of `o` but such that mutations of
     * one object are not visible within the other.
     */
    public static Object deepClone(Object o) {
        if (o == null) {
            return null;
        }
        if (o instanceof PValue<?>) {
            return ((PValue<?>)o).deepClone();
        }

        Class<?> clazz = o.getClass();
        if (clazz == Boolean.class)
            return cloneBoolean((Boolean)o);
        if (clazz == Integer.class)
            return cloneInteger((Integer)o);
        if (clazz == Long.class)
            return cloneLong((Long)o);
        if (clazz == Float.class)
            return cloneFloat((Float)o);
        if (clazz == String.class)
            return cloneString((String)o);
        if (clazz == ArrayList.class)
            return cloneList((ArrayList<?>) o);
        if (clazz == HashMap.class)
            return cloneMap((HashMap<?, ?>) o);
        if (clazz == LinkedHashSet.class)
            return cloneSet((LinkedHashSet<?>) o);
        if (Enum.class.isAssignableFrom(clazz))
            return cloneEnum((Enum) o);


        throw new UncloneableValueException(clazz);
    }
}
