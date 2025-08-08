package pobserve.metrics;


import pobserve.logger.PObserveLogger;
import pobserve.runtime.Monitor;
import pobserve.runtime.events.PEvent;

import edu.umd.cs.findbugs.annotations.SuppressFBWarnings;
import java.io.BufferedWriter;
import java.io.FileWriter;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Paths;
import java.util.HashMap;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.function.Consumer;
import lombok.Getter;
import lombok.Setter;

import static pobserve.config.PObserveConfig.getPObserveConfig;

/**
 * PObserveMetrics class keeps track of PObserve job metrics
 */
@Getter
public class PObserveMetrics {
    private static final PObserveMetrics pObserveMetrics;
    public PObserveMetrics() {
        metricsMap = new ConcurrentHashMap<>();
        for (String metricName: MetricConstants.getMetricsList()) {
            metricsMap.put(metricName, new AtomicInteger(0));
        }

        Consumer<PEvent<?>> consumer = getPObserveConfig().getSpecificationSupplier().get();
        List<Class<? extends pobserve.runtime.events.PEvent<?>>> eventTypes = ((Monitor<?>) consumer).getEventTypes();
        eventMetrics = new HashMap<>();
        eventTypes.forEach(eventType -> {
            String type = eventType.getSimpleName();
            eventMetrics.put(type, new EventMetrics(type));
        });
    }

    static {
        try {
            pObserveMetrics = new PObserveMetrics();
        } catch (Exception e) {
            throw new RuntimeException("Exception occurred in creating PObserveMetrics instance", e);
        }
    }

    @SuppressFBWarnings("MS_EXPOSE_REP")
    public static synchronized PObserveMetrics getPObserveMetrics() {
        return pObserveMetrics;
    }

    private ConcurrentHashMap<String, AtomicInteger> metricsMap;

    @Setter
    private long startTime;

    @Setter
    private long endTime;

    private final HashMap<String, EventMetrics> eventMetrics;

    public void addMetric(String name, Integer value) {
        metricsMap.get(name).addAndGet(value);
    }

    public void updateEventMetrics(String eventType, long execTime) {
        eventMetrics.get(eventType).update(execTime);
    }

    public void outputMetricsSummary() {
        int totalVerified = 0;

        for (EventMetrics em : eventMetrics.values()) {
            totalVerified += em.getVerified();
        }

        StringBuilder runSummary = new StringBuilder();
        runSummary.append("\nTotal time taken = " + ((float) (endTime - startTime) / (float) 60000) + " min\n");
        runSummary.append("\n---------------------------------------------------------------------------------------------------------\n");
        runSummary.append(String.format("%-23s  %-23s  %-23s  %-23s%n", "Total Events Read", "Total Verified Events", "Total Verified Keys", "Total Partition Keys"));
        runSummary.append("---------------------------------------------------------------------------------------------------------\n");
        runSummary.append(String.format("%-23s  %-23s  %-23s  %-23s%n",
                metricsMap.get(MetricConstants.TOTAL_EVENTS_READ).get(),
                totalVerified,
                metricsMap.get(MetricConstants.TOTAL_VERIFIED_KEYS).get(),
                metricsMap.get(MetricConstants.TOTAL_PARTITION_KEYS).get())).append("\n");

        runSummary.append("\n------------------------------------------------------------------------------------------------------\n");
        runSummary.append(String.format("%-23s  %-23s  %-28s  %-23s%n", "Total Parser Errors", "Total Spec Errors", "Total Out of Order Errors", "Total Unknown Errors"));
        runSummary.append("------------------------------------------------------------------------------------------------------\n");
        runSummary.append(String.format("%-23s  %-23s  %-28s  %-23s%n",
                metricsMap.get(MetricConstants.TOTAL_UNKNOWN_ERRORS).get(),
                metricsMap.get(MetricConstants.TOTAL_SPEC_ERRORS).get(),
                metricsMap.get(MetricConstants.TOTAL_EVENT_OUT_OF_ORDER_ERRORS).get(),
                metricsMap.get(MetricConstants.TOTAL_UNKNOWN_ERRORS).get())).append("\n");

        PObserveLogger.info(runSummary.toString());

        String eventsSummary = buildEventsSummary();
        logMetricsToFile(runSummary.toString(), eventsSummary);
    }

    private void logMetricsToFile(String runSummary, String eventsSummary) {
        String outputFilePath = Paths.get(getPObserveConfig().getOutputDir().getAbsolutePath(),  "PObserveMetrics.txt").toString();
        try {
            FileWriter fw = new FileWriter(outputFilePath, StandardCharsets.UTF_8);
            BufferedWriter bw = new BufferedWriter(fw);
            bw.write(runSummary);
            bw.write(eventsSummary);
            bw.close();
            fw.close();
        } catch (IOException e) {
            throw new RuntimeException("Exception occurred while writing metrics to output file", e);
        }
    }

    private String buildEventsSummary() {
        StringBuilder summary = new StringBuilder();
        summary.append("-------------------------------------------------------------------------------------------------------").append("\n");
        summary.append(String.format("%-30s  %-20s  %-20s  %-20s%n", "Event Type", "Total Verified", "Avg Exec Time (ms)", "Max Exec Time (ms)"));
        summary.append("-------------------------------------------------------------------------------------------------------").append("\n");
        for (EventMetrics em : eventMetrics.values()) {
            summary.append(em.getSummary()).append("\n");
        }
        return summary.toString();
    }
}
