package prt.values;

import java.util.Iterator;
import java.util.LinkedHashSet;
import java.util.NoSuchElementException;

public class SetIndexing {
    // A helper that walks a LinkedHashSet's iterator to get the `i`th value in the set.  This is
    // implemented here as J.u.LinkedHashSet has no equivalent of C# HashSet::elementAt() method.
    public static <T> T elementAt(LinkedHashSet<T> s, int i) throws NoSuchElementException
    {
        if (i < 0) throw new NoSuchElementException();
        Iterator<T> it = s.iterator();
        T ret = it.next();

        while (i > 0) {
            ret = it.next();
            i--;
        }

        return ret;
    }
}
