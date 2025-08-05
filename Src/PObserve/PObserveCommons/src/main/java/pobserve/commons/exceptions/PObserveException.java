package pobserve.commons.exceptions;

import java.util.Map;

import static java.util.Map.entry;

/** This class is used for throwing exceptions at two levels: error and warning. */
public class PObserveException extends Exception {

  /** Exception levels: error and warning. */
  public enum ExceptionLevel {
    /** Error exception level. */
    ERROR,
    /** Warning exception level. */
    WARNING
  }

  private static final String ANSI_RED = "\u001B[31m";
  private static final String ANSI_YELLOW = "\u001B[33m";
  private static final String ANSI_RESET = "\u001B[0m";

  private static final Map<ExceptionLevel, String> EXCEPTION_LEVEL_COLOR_MAP =
      Map.ofEntries(
          entry(ExceptionLevel.ERROR, ANSI_RED), entry(ExceptionLevel.WARNING, ANSI_YELLOW));

  /**
   * Method for throwing exceptions.
   *
   * @param message is the exception message.
   * @param exceptionLevel is the level of exception which is either error or warning.
   * @param cause is the cause of the exception.
   */
  public PObserveException(String message, ExceptionLevel exceptionLevel, Throwable cause) {
    super(EXCEPTION_LEVEL_COLOR_MAP.get(exceptionLevel) + message + ANSI_RESET, cause);
  }
}
