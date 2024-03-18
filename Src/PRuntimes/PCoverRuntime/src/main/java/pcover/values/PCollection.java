package pcover.values;

/**
 * Represents the base class for PValues that are collections.
 */
public abstract class PCollection extends PValue<PCollection> {
    /**
     * Get the size of the collection.
     * @return size
     */
    public abstract int size();

    /**
     * Check if the collection contains the given item.
     * @param item item to check for.
     * @return true if the collection contains the item, otherwise false
     */
    public abstract boolean contains(PValue<?> item);
}
