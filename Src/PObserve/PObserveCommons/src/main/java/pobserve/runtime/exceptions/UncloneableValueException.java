package pobserve.runtime.exceptions;

public class UncloneableValueException extends RuntimeException {
    public UncloneableValueException(Class<?> c) {
        super(String.format("No clone operation for class " + c.getName()));
    }
}
