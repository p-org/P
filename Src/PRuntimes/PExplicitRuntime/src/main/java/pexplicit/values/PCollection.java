package pexplicit.values;

/**
 * Represents the base class for PValues that are collections.
 */
public interface PCollection<T> {
    /**
     * Get the size of the collection.
     *
     * @return size
     */
    public abstract PInt size();

    /**
     * Check if the collection contains the given item.
     *
     * @param item item to check for.
     * @return true if the collection contains the item, otherwise false
     */
    public abstract PBool contains(T item);
}
