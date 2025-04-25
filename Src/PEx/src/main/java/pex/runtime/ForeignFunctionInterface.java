package pex.runtime;

import java.util.List;
import java.util.function.Consumer;
import java.util.function.Function;

public class ForeignFunctionInterface {
    /**
     * Invoke a foreign function with a void return type
     *
     * @param fn   function to invoke
     * @param args arguments
     */
    public static void accept(Consumer<List<Object>> fn, Object... args) {
        fn.accept(List.of(args));
    }

    /**
     * Invoke a foreign function with a non-void return type
     *
     * @param fn   function to invoke
     * @param args arguments
     * @return the return value of the function
     */
    public static Object apply(Function<List<Object>, Object> fn, Object... args) {
        return fn.apply(List.of(args));
    }
}
