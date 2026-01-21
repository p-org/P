package pobserve.executor;

import pobserve.commons.PObserveEvent;
import pobserve.config.SourceInputKind;
import pobserve.metrics.MetricConstants;
import pobserve.report.PObserveError;
import pobserve.report.TrackErrors;
import pobserve.source.file.PObserveFileReader;
import pobserve.source.file.SerializedEventFileReader;

import java.io.File;
import java.util.Iterator;
import java.util.List;
import java.util.PriorityQueue;
import java.util.stream.Stream;
import lombok.AllArgsConstructor;

import static pobserve.config.PObserveConfig.getPObserveConfig;
import static pobserve.metrics.PObserveMetrics.getPObserveMetrics;

/**
 * PObserveExecutor class executes the PObserve job
 */
public class PObserveExecutor {

    public void run() {
        getPObserveMetrics().setStartTime(System.currentTimeMillis());
        try (var inputPObserveEventStream = ReadInputsToPObserveEvents(getPObserveConfig().getLogFiles())) {
            // partition pobserve events into keyed streams
            var partitionedEvents = PartitionEventStream.partitionByKey(inputPObserveEventStream);
            getPObserveMetrics().addMetric(MetricConstants.TOTAL_PARTITION_KEYS, partitionedEvents.size());
            // run the monitor on each partitioned pobserve event stream
            partitionedEvents.entrySet().stream().parallel()
                    .forEach(keyedStream ->
                                     CheckPartitionedEventStream.check(keyedStream.getKey(), keyedStream.getValue().stream()));
        } catch (Exception e) {
            getPObserveMetrics().addMetric(MetricConstants.TOTAL_UNKNOWN_ERRORS, 1);
            TrackErrors.addError(new PObserveError(e));
        } finally {
            getPObserveMetrics().setEndTime(System.currentTimeMillis());
        }
    }

    private Stream<? extends PObserveEvent> ReadInputsToPObserveEvents(List<File> logFiles) throws Exception {
        if (getPObserveConfig().getInputKind() == SourceInputKind.SERIALIZEDEVENTS) {
            var inputStream =
                    logFiles.stream().parallel().flatMap(file -> SerializedEventFileReader.readEventsFromFile(file));
            return inputStream;
        }

        if (getPObserveConfig().isAssumeInputFilesAreSorted()) {
            // input files are sorted so lets read the files sequentially doing a sorted-merge on the fly
            return MergeIntoSortedStream(logFiles);
        } else {
            // we read all the files in parallel and generate a single stream out of it.
            var inputStream =
                    logFiles.stream().parallel().flatMap(file -> PObserveFileReader.getFileReader().readFile(file));
            // parse inputs into PObserve events
            return ParseEventStream.parseToPObserveEvents(inputStream);
        }
    }
    @AllArgsConstructor
    private static class StreamWithPriority {
        PObserveEvent priority;
        Iterator<? extends PObserveEvent> streamiterator; // the stream itself
    }
    private Stream<? extends PObserveEvent> MergeIntoSortedStream(List<File> logFiles) {

        // create a list of stream iterators, one iterator per file
        // create a priority queue of stream iterators, sorted by the time at the front of the stream
        PriorityQueue<StreamWithPriority> sortedStreamsList =
                new PriorityQueue<>((a, b) -> a.priority.getTimestamp().compareTo(b.priority.getTimestamp()));
        logFiles.forEach(file -> {
            try {
                var fileStreamIterator =
                        ParseEventStream.parseToPObserveEvents(PObserveFileReader.getFileReader().readFile(file)).iterator();
                if (fileStreamIterator.hasNext()) {
                    var firstTimestampEvent = fileStreamIterator.next();
                    sortedStreamsList.add(new StreamWithPriority(firstTimestampEvent,
                            fileStreamIterator));
                }
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });

        // merge the sorted streams into a single stream, sorted by the time at the front of the stream.
        // we use a priority queue to keep the streams sorted by the time at the front of the stream.
        Stream.Builder<PObserveEvent> mergedStreamBuilder = Stream.builder();
        while (sortedStreamsList.isEmpty() == false) {
            var streamWithPriority = sortedStreamsList.poll();
            mergedStreamBuilder.accept(streamWithPriority.priority);
            if (streamWithPriority.streamiterator.hasNext()) {
                var nextTimestampEvent = streamWithPriority.streamiterator.next();
                sortedStreamsList.add(new StreamWithPriority(nextTimestampEvent, streamWithPriority.streamiterator));
            }
        }
        return mergedStreamBuilder.build();
    }
}
