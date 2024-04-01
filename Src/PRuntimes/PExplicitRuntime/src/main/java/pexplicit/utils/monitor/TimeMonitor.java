package pexplicit.utils.monitor;

import java.time.Duration;
import java.time.Instant;
import java.util.concurrent.TimeoutException;
import lombok.Getter;

/**
 * Represents the time monitor to track runtime and enforce timeout
 */
public class TimeMonitor {
  /** Stores the start time to track total runtime */
  @Getter private static Instant start;

  // time limit in seconds (0 means infinite)
  private static double timeLimit;

  /** Stores the beginning time to track when a time interval begins */
  private static Instant begin;

  public static void setup(double tl) {
    start = Instant.now();
    begin = Instant.now();
    timeLimit = tl;
  }

  public static double getRuntime() {
    return findInterval(start);
  }

  public static void startInterval() {
    begin = Instant.now();
  }

  public static double stopInterval() {
    return Duration.between(begin, Instant.now()).toMillis() / 1000.0;
  }

  public static double findInterval(Instant intervalBegin) {
    return Duration.between(intervalBegin, Instant.now()).toMillis() / 1000.0;
  }

  public static void checkTimeout() throws TimeoutException {
    if (timeLimit != 0) {
      double elapsedTime = getRuntime();
      if (elapsedTime > timeLimit) {
        throw new TimeoutException(
            String.format("Max time limit reached. Runtime: %.1f seconds", elapsedTime));
      }
    }
  }
}
