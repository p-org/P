package p.runtime.values.exceptions;

import p.runtime.PRuntimeException;
import p.runtime.values.PMap;
import p.runtime.values.PValue;

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
