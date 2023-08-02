package psym.runtime.values.exceptions;

import java.util.Map;
import psym.runtime.values.PValue;

public class KeyNotFoundException extends PRuntimeException {

    public KeyNotFoundException(PValue<?> key, Map<PValue<?>, PValue<?>> map) {
        super(String.format("Key %s not found in Map: %s", key, map.toString()));
    }
}
