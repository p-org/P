package psymbolic.utils;

import lombok.Getter;

import java.time.Duration;
import java.time.Instant;
import java.util.concurrent.TimeoutException;

public class TimeMonitor {
    /**
     * Represents the private field that refers to the singleton object
     */
    private static TimeMonitor timeMonitorObject;

    /**
     * Stores the start time to track total runtime
     */
    @Getter
    private Instant start;

    /**
     * Stores the beginning time to track when a time interval begins
     */
    private Instant begin;
    // time limit in seconds (0 means infinite)
    private double timeLimit;

    private TimeMonitor(double tl) {
        this.start = Instant.now();
        this.begin = Instant.now();
        this.timeLimit = tl;
    }

    public static TimeMonitor getInstance() {
        assert(timeMonitorObject != null);

        // returns the singleton object
        return timeMonitorObject;
    }

    public static void setup(double tl) {
        timeMonitorObject = new TimeMonitor(tl);
    }

    public double getRuntime() {
        return findInterval(start);
    }

    public void startInterval() {
        begin = Instant.now();
    }

    public double stopInterval() {
        return Duration.between(begin, Instant.now()).toMillis() / 1000.0;
    }

    public double findInterval(Instant intervalBegin) {
        return Duration.between(intervalBegin, Instant.now()).toMillis() / 1000.0;
    }

    public void checkTimeout() throws TimeoutException {
        if (timeLimit != 0) {
            double elapsedTime = getRuntime();
            if (elapsedTime > timeLimit) {
                throw new TimeoutException(String.format("Max time limit reached. Runtime: %.1f seconds", elapsedTime));
            }
        }
    }

}
