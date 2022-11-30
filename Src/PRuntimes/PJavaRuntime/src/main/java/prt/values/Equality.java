package prt.values;

import prt.events.PEvent;
import prt.exceptions.IncomparableValuesException;

import java.util.*;

public class Equality {
    public static <T extends Comparable<T>> int compare(T o1, T o2) {
        // Just to keep things well-defined, treat null as a minimal value.
        if (o1 == null) {
            return o2 == null ? 0 : -1;
        }
        if (o2 == null) {
            return 1;
        }

        // String.compareTo(String) doesn't return a value on [-1, 1] so clamp out-of-range values.
        int ret = o1.compareTo(o2);
        if (ret > 1) ret = 1;
        if (ret < -1) ret = -1;

        return ret;
    }

    private static <T1, T2> boolean deepLinkedHashSetEquals(LinkedHashSet<T1> a1, LinkedHashSet<T2> a2) {
        // A direct equality call matches the behaviour of Plang.CSharpRuntime.Values.PrtSet.Equals .
        return a1.equals(a2);
    }

    private static <T1, T2> boolean deepArrayEquals(ArrayList<T1> a1, ArrayList<T2> a2) {
        if (a1.size() != a2.size()) {
            return false;
        }
        for (int i = 0; i < a1.size(); i++) {
            T1 v1 = a1.get(i);
            T2 v2 = a2.get(i);
            if (v1.getClass() != v2.getClass()) {
                return false;
            }
            if (!deepEquals(v1, v2)) {
                return false;
            }
        }
        return true;
    }


    private static <K1, V1, K2, V2> boolean deepHashMapEquals(HashMap<K1, V1> m1, HashMap<K2, V2> m2) {
        if (!m1.keySet().equals(m2.keySet())) {
            return false;
        }
        for (K1 k : m1.keySet()) {
            V1 v1 = m1.get(k);
            V2 v2 = m2.get(k);
            if (!deepEquals(v1, v2)) {
                return false;
            }
        }
        return true;
    }

    private static <T extends PValue<T>, U extends PValue<U>> boolean deepPValueEquals(PValue<T> t1, PValue<U> t2) {
        if (t1.getClass() != t2.getClass()) {
            return false;
        }
        return t1.deepEquals((T)t2);
    }


    public static boolean deepEquals(Object o1, Object o2) {
        // Only a null is equal to a null, to keep things well-defined.
        if (o1 == null) {
            return o2 == null;
        }
        if (o2 == null) {
            return false;
        }

        try {
            // For PValues, defer to their `deepEquals()` method (which may recursively call
            // back into `Values.deepEquals()`.
            if (o1 instanceof PValue<?> && o2 instanceof PValue<?>) {
                return deepPValueEquals((PValue<?>) o1, (PValue<?>) o2);
            }

            if (o1 instanceof PEvent<?> && o2 instanceof PEvent<?>) {
                return o1.equals(o2);
            }

            // Otherwise, dispatch on the classes.
            Class<?> c1 = o1.getClass();
            Class<?> c2 = o2.getClass();

            if (c1 == Boolean.class && c2 == Boolean.class)
                return compare((Boolean) o1, (Boolean) o2) == 0;
            if (c1 == Integer.class && c2 == Integer.class)
                return compare((Integer) o1, (Integer) o2) == 0;
            if (c1 == Long.class && c2 == Long.class)
                return compare((Long) o1, (Long) o2) == 0;
            if (c1 == Float.class && c2 == Float.class)
                return compare((Float) o1, (Float) o2) == 0;
            if (c1 == String.class && c2 == String.class)
                return compare((String) o1, (String) o2) == 0;
            if (c1 == ArrayList.class && c2 == ArrayList.class)
                return deepArrayEquals((ArrayList<?>) o1, (ArrayList<?>) o2);
            if (c1 == HashMap.class && c2 == HashMap.class)
                return deepHashMapEquals((HashMap<?, ?>) o1, (HashMap<?, ?>) o2);
            if (c1 == LinkedHashSet.class && c2 == LinkedHashSet.class)
                return deepLinkedHashSetEquals((LinkedHashSet<?>) o1, (LinkedHashSet<?>) o2);

            if (Enum.class.isAssignableFrom(c1)) {
                if (c1 == c2) {
                    return ((Enum<?>)o1).ordinal() == ((Enum<?>)o2).ordinal();
                }
            }
            throw new IncomparableValuesException(c1, c2);

        } catch (ClassCastException e) {
            // The C# P runtime is pretty permissive about comparing different types (no runtime exception,
            // they just evaluate to false) so we do the same here in case we are passed incomparable types.
            return false;
        }
    }

}
