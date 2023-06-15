package psym.runtime.values.exceptions;

import psym.runtime.values.PValue;

import java.util.Map;

public class KeyNotFoundException extends PRuntimeException {

    public KeyNotFoundException(PValue<?> key, Map<PValue<?>, PValue<?>> map) {
        super(String.format("Key %s not found in Map: %s", key, map.toString()));
    }
}
