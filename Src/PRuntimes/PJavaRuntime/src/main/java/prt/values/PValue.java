package prt.values;

public interface PValue<P extends PValue<P>> {
    /**
     * Performs a deep copy of a P tuple by
     * (The performance difference between a hand-rolled deep copy and using serializers
     * appears to be significant[1], so doing the former seems to be a good idea.)
     * [1]: <a href="https://www.infoworld.com/article/2077578/java-tip-76--an-alternative-to-the-deep-copy-technique.html">...</a>
     *
     * @return a structurally-equivalent version of `this` but such that mutations of
     * one object are not visible within the other.
     */
    P deepClone();

    /**
     * Performs a deep equality check against another Object.
     * @param o2 The other object.
     * @return If this and o2 are object of the same class, and their fields are deeply equal to each other's.
     */
    boolean deepEquals(P o2);
}
