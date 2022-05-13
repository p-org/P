package psymbolic.valuesummary.util;

import java.io.Serializable;
import java.util.function.BiFunction;

@FunctionalInterface
public interface SerializableBiFunction<T, U, R> extends BiFunction<T, U, R>, Serializable {
}
