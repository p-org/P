package prt.exceptions;

/**
 * Exception: Two values are not comparable
 */
public class IncomparableValuesException extends RuntimeException {
    public IncomparableValuesException(Class<?> c1, Class<?> c2) {
        super(String.format("Can't compare values of type %s and %s", c1.getName(), c2.getName()));
    }
}
