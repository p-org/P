package psymbolic.runtime.values.exceptions;

import psymbolic.runtime.values.PValue;

import java.util.Map;

public class KeyNotFoundException extends PRuntimeException {
    public KeyNotFoundException(String message) {
        super(message);
    }

    public KeyNotFoundException(PValue<?> key, Map<PValue<?>, PValue<?>> map)
    {
        super(String.format("Key %s not found in Map: %s", key, map.toString()));
    }
}
