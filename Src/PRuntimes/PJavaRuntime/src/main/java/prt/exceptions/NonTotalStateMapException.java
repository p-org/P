package prt.exceptions;

/**
 * Thrown when a Monitor tries to ready itself when not every value in the StateKey
 * enum is set.
 */
public class NonTotalStateMapException extends RuntimeException {
   public NonTotalStateMapException(Enum<?> missingKey) {
       super(String.format("State map is not total (missing addState() call for %s)", missingKey.name()));
   }
}
