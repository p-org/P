package prt.values;

import prt.exceptions.UncloneableValueException;

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

    public static Boolean deepClone(Boolean b) {
        return b; //already immutable!  No cloning necessary.
    }

    public static Long deepClone(Long l) {
        return l; //already immutable!  No cloning necessary.
    }

    public static Float deepClone(Float f) {
        return f; //already immutable!  No cloning necessary.
    }

    public static String deepClone(String s) {
        return s; //already immutable!  No cloning necessary.
    }


    public static <T> ArrayList<T> deepClone(ArrayList<T> a) {
        if (a == null) return null;

        ArrayList<T> cloned = new ArrayList<>();
        cloned.ensureCapacity(a.size());
        for (T val : a) {
            cloned.add(deepClone(val));
        }
        return cloned;
    }

    public static <T> LinkedHashSet<T> deepClone(LinkedHashSet<T> s)
    {
        if (s == null) return null;

        LinkedHashSet<T> cloned = new LinkedHashSet<>();
        for (T val : s) {
            cloned.add(deepClone(val));
        }
        return cloned;
    }

    public static <T,U> HashMap<T, U> deepClone(HashMap<T, U> m)
    {
        if (m == null) return null;

        HashMap<T, U> cloned = new HashMap<>();
        for (Map.Entry<T,U> e : m.entrySet()) {
            T k = deepClone(e.getKey()); // TODO: shouldn't keys be immutable as-is?  Can we elide this deep clone?
            U v = deepClone(e.getValue());
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
    public static <T> T deepClone(T o) {
        if (o == null) {
            return null;
        }

        Class<?> clazz = o.getClass();
        // Immutable types require no special cloning operation.
        if (clazz == Boolean.class)
            return o;
        if (clazz == Long.class)
            return o;
        if (clazz == Float.class)
            return o;
        if (clazz == String.class)
            return o;
        if (o instanceof Enum<?>)
            return o;

        // Collection types necessitate recursive cloning of its elements.
        if (o instanceof PValue<?>)
            return (T) ((PValue<?>)o).deepClone();

        if (clazz == ArrayList.class)
            return (T) deepClone((ArrayList<?>) o);
        if (clazz == HashMap.class)
            return (T) deepClone((HashMap<?, ?>) o);
        if (clazz == LinkedHashSet.class)
            return (T) deepClone((LinkedHashSet<?>) o);

        throw new UncloneableValueException(clazz);
    }
}
