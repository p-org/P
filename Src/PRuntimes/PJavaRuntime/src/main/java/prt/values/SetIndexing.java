package prt.values;

import java.lang.ref.WeakReference;
import java.util.*;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Handles producing the `ith` element of a LinkedHashSet in as efficient a manner as possible.
 * This is implemented outside the collection type itself, since in contrast to C#'s `HashSet::elementAt()`,
 * Java's `java.util.LinkedHashSet` has no built-in way to iterate to the ith element in the set.
 */
public class SetIndexing {


    /**
     * Cached iteration state: we store the most recently-used set and its iterator, and the index
     * that the iterator last read from.  The set and iterator are stored as weak references,
     * meaning that the garbage collector may choose to evict them during collection.  In this case,
     * we will have to recompute the state from scratch, but we aren't going to retain a reference to a Set
     * that is no longer reachable from P code.
     */
    private static class CachedState {
        private WeakReference<LinkedHashSet<?>> cached_set;
        private WeakReference<Iterator<?>> cached_it;
        private long it_idx;

        public LinkedHashSet<?> getCachedSet() {
            if (cached_set == null) {
                return null;
            }
            return cached_set.get();
        }
        public Iterator<?> getIterator() {
            if (cached_it == null) {
                return null;
            }
            return cached_it.get();
        }
        public long getIdx() {
            return it_idx;
        }

        public void setState(LinkedHashSet<?> s, Iterator<?> it, long idx) {
            if (it.hasNext()) {
                cached_set = new WeakReference<>(s);
                cached_it = new WeakReference<>(it);
                it_idx = idx;
            } else {
                // If the state points past the end of the set (i.e. we accessed the
                // final element), save us some steps and just zap out the whole state.
                cached_set = null;
                cached_it = null;
                it_idx = -1;
            }
        }
    }

    private static final ThreadLocal<CachedState> state = ThreadLocal.withInitial(() -> new CachedState());

    private static final AtomicLong slowPathHits = new AtomicLong();
    private static final AtomicLong fastPathHits = new AtomicLong();

    /**
     * Returns the proportion of set accesses that were able to use the cached iterator.
     * @return
     */
    public static double GetIteratorCacheHitRate() {
        return fastPathHits.get() / (double)(slowPathHits.get() + fastPathHits.get());
    }

    /**
     * Sets with elements smaller than this bypass the cache, since the overhead of caching exceeds the small O(n)
     * cost of traversing from the beginning.
     */
    public static final int MIN_SETSIZE = 15;

    /**
     * Returns the `i`th element in the set.
     * @param s The set to iterate through.
     * @param i The index of the element.
     * @return The ith element.
     * @throws NoSuchElementException on out-of-bounds accesses.
     */
    public static <T> T elementAt(LinkedHashSet<T> s, long i) throws NoSuchElementException
    {
        /* If the index is sufficiently close to the start of the set, the overhead of manipulating
         * the thread-local cache state is greater than just walking the linked list.
         * TODO: MIN_SETSIZE was estimated from benchmarking on a Mac.  Doing this on prod hardware would be better.
         */
        if (i <= MIN_SETSIZE) {
            return elementAtSlowIter(s, s.iterator(), i);
        }

        CachedState c = state.get();
        LinkedHashSet<?> cached_set = c.getCachedSet();
        Iterator<?> cached_it = c.getIterator();
        long cached_i = c.getIdx();

        /* If our cache is not fully populated, defer to the slow path. */
        if (cached_set == null || cached_it == null) {
            return elementAtSlow(s, i);
        }

        /* Even if our cache is fully populated, we still defer to the slow path if... */

        /* ... the supplied set is pointer-unequal from the previously-cached one. */
        if (cached_set != s) {
            return elementAtSlow(s, i);
        }

        /* ... the index that the P program wants isn't reachable from the iterator. */
        if (cached_i >= i) {
            return elementAtSlow(s, i);
        }

        /* Fast path: we can reuse the cached state safely! */
        try {
            // We now know the type of the cached state, since the caller's Set is pointer-equal to the cached Set,
            // and we never set the iterator state without also setting the Set that backs it, so this upcast is safe.
            Iterator<T> it = (Iterator<T>) cached_it;

            return elementAtFast(s, it, cached_i, i);
        } catch (ConcurrentModificationException e) {
            /* As P collections are immutable, we should never see a modified set from generated code.  However, there
             * is always a chance that a Set could be modified in foreign code, so we still need to handle it. */
            return elementAtSlow(s, i);
        }
    }

    /**
     * Returns the `ith` element given the current iterator, already pointing to some particular index.
     * As a side-effect, additionally caches the given set, index, and derived iterator to amortize
     * subsequent accesses.
     *
     * This method can, additionally, throw a ConcurrentModificationException if, in between previous element
     * accesses, the underlying state of the set has been changed.
     */
    private static <T> T elementAtFast(LinkedHashSet<T> s, Iterator<T> it, long current_idx, long i)
            throws ConcurrentModificationException, NoSuchElementException
    {
        assert(current_idx < i); // Should be checked by the caller.
        fastPathHits.getAndIncrement();

        T ret = null;
        while (current_idx < i) {
            ret = it.next();
            current_idx++;
        }

        state.get().setState(s, it, i);
        return ret;
    }

    /**
     * Returns the `i`th element in `s` by constructing a new iterator and advancing it `i` times
     * to the desired element.  As a side-effect, additionally caches the given set, index, and
     * derived iterator to amortize subsequent accesses.
     */
    private static <T> T elementAtSlow(LinkedHashSet<T> s, long i) throws NoSuchElementException
    {
        Iterator<T> it = s.iterator();
        T ret = elementAtSlowIter(s, it, i);

        state.get().setState(s, it, i);
        return ret;
    }


    private static <T> T elementAtSlowIter(LinkedHashSet<T> s, Iterator<T> it, long i) throws NoSuchElementException {
        if (i < 0 || i >= s.size()) throw new NoSuchElementException(Long.toString(i));
        slowPathHits.getAndIncrement();

        T ret = it.next();

        while (i > 0) {
            ret = it.next();
            i--;
        }
        return ret;
    }
}
