package pcover.values.exceptions;

import java.util.Map;

import pcover.utils.exceptions.PCoverRuntimeException;
import pcover.values.PValue;

/**
 * Thrown when a key is not found in a PMap
 */
public class KeyNotFoundException extends PCoverRuntimeException {

    /**
     * Constructs a new KeyNotFoundException with the given key and map.
     */
    public KeyNotFoundException(PValue<?> key, Map<PValue<?>, PValue<?>> map) {
        super(String.format("Key %s not found in Map: %s", key, map.toString()));
    }
}
