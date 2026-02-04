package pobserve.metrics;

import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;

/**
 * EventMetrics class keeps track of event metrics
 */
public class EventMetrics {
    public EventMetrics(String eventType) {
        type = eventType;
        verified = new AtomicInteger(0);
        totalExecTime = new AtomicLong(0);
        maxExecTime = new AtomicLong(0);
    }

    private final AtomicInteger verified;
    private final AtomicLong totalExecTime;
    private final AtomicLong maxExecTime;
    private final String type;

    public void update(long execTime) {
        verified.getAndIncrement();
        totalExecTime.getAndAdd(execTime);
        maxExecTime.getAndAccumulate(execTime, Math::max);
    }

    public float getAvgExecTime() {
        return verified.get() > 0 ? (float) totalExecTime.get() / verified.get() : 0;
    }

    public long getMaxExecTime() {
        return maxExecTime.get();
    }

    public int getVerified() {
        return verified.get();
    }

    public String getSummary() {
        return String.format("%-30s  %-20d  %-20f  %-20s", type, verified.get(), getAvgExecTime(), getMaxExecTime());
    }
}
