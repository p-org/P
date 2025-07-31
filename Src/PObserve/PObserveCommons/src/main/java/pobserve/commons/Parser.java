package pobserve.commons;

import java.io.Serializable;
import java.util.function.Function;
import java.util.stream.Stream;

public interface Parser<E> extends Function<Object, Stream<PObserveEvent<E>>>, Serializable {
    /*
     * The log entry delimiter that the parser requires; defaults to \n for one log per line
     */
    default String getLogDelimiter() {
        return "\n";
    }

    /*
     * An optional configuration string that can be passed from PObserve to the parser; defaults to ignore it
     */
    default void setConfiguration(String configuration) {
    }
}
