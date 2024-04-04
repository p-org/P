package pexplicit.utils.serialize;

import java.io.Serializable;

@FunctionalInterface
public interface SerializableBiFunction<T, U> extends Serializable {
    void apply(T t, U u);
}
