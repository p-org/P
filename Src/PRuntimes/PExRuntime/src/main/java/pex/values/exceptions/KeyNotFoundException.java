package pex.values.exceptions;

import pex.utils.exceptions.BugFoundException;
import pex.values.PValue;

import java.util.Map;

/**
 * Thrown when a key is not found in a PMap
 */
public class KeyNotFoundException extends BugFoundException {

    /**
     * Constructs a new KeyNotFoundException with the given key and map.
     */
    public KeyNotFoundException(PValue<?> key, Map<PValue<?>, PValue<?>> map) {
        super(String.format("Key %s not found in Map: %s", key, map.toString()));
    }
}
