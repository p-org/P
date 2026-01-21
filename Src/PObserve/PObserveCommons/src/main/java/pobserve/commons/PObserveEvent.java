package pobserve.commons;

import javax.validation.constraints.NotNull;
import java.io.Serializable;
import java.time.Instant;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeFormatterBuilder;
import java.util.Objects;

/**
 * PObserveEvent is the timestamped event processed by the PObserve system.
 * PObserveEvent is a wrapper over the P event which is observed by the P specification.
 * PObserveEvent has the required information used by the rest of the
 * PObserve system to route the events to specification (e.g., timestamp, partitionKey, etc).
 */
public class PObserveEvent<E> implements Comparable<PObserveEvent<E>>, Serializable {

  private static final long serialVersionUID = 1L;

  @NotNull
  /*
   * partitionKey is the key used to partition the events stream.
   * The assumption is that there is a separate P specification that is checked against each partitioned event stream
   */
  private String partitionKey;

  /*
   * timestamp is the timestamp of the event, this is mostly gathered from the log line.
   */
  private Instant timestamp;

  /*
   * event represents the event that is consumed by the specification.
   */
  private E event;

  /*
    additional payload associated with the event that is needed by the PObserve service or the specification
   */
  private Object customPayload;

  /*
   * logLine is the original logLine received as part of the logs.
   * This is used to reconstruct the original log when reporting error counter example.
   */
  private transient String logLine = null;

  /**
   * Constructor for events with a timestamp but wrapped event.
   *
   * @param key is partitionKey of the PObserveEvent object.
   * @param timestamp is timestamp of the PObserveEvent object.
   */
  public PObserveEvent(@NotNull String key, Long timestamp) {
    this(Objects.requireNonNull(key), timestamp, (E) null);
  }

  /**
   * Constructor for events with a timestamp but no timestamp interval and no log line.
   *
   * @param key is partitionKey of the PObserveEvent object.
   * @param event is wrapped event used by the user.
   * @param timestamp is timestamp of the PObserveEvent object.
   */
  public PObserveEvent(@NotNull String key, Long timestamp, E event) {
    this.partitionKey = Objects.requireNonNull(key);
    this.timestamp = Instant.ofEpochMilli(timestamp);
    this.event = event;
  }

  public PObserveEvent(@NotNull String key, Instant timestamp, E event) {
    this.partitionKey = Objects.requireNonNull(key);
    this.timestamp = timestamp;
    this.event = event;
  }

  /**
   * Constructor for events with a timestamp but no timestamp interval.
   *
   * @param key is partitionKey of the PObserveEvent object.
   * @param event is wrapped event used by the user.
   * @param timestamp is timestamp of the PObserveEvent object.
   * @param logLine is the original logLine received as part of the logs
   */
  public PObserveEvent(@NotNull String key, Long timestamp, E event, String logLine) {
    this.partitionKey = Objects.requireNonNull(key);
    this.timestamp = Instant.ofEpochMilli(timestamp);
    this.event = event;
    this.logLine = logLine;
  }

  public PObserveEvent(@NotNull String key, Instant timestamp, E event, String logLine) {
    this.partitionKey = Objects.requireNonNull(key);
    this.timestamp = timestamp;
    this.event = event;
    this.logLine = logLine;
  }

  /**
   * Constructor for events with a timestamp but no timestamp interval.
   *
   * @param key is partitionKey of the PObserveEvent object.
   * @param event is wrapped event used by the user.
   * @param timestamp is timestamp of the PObserveEvent object.
   * @param logLine is the original logLine received as part of the logs
   * @param customPayload can be any arbitary payload used when needed
   */
  public PObserveEvent(@NotNull String key, Long timestamp, E event, String logLine, Object customPayload) {
    this.partitionKey = Objects.requireNonNull(key);
    this.timestamp = Instant.ofEpochMilli(timestamp);
    this.event = event;
    this.logLine = logLine;
    this.customPayload = customPayload;
  }

  public PObserveEvent(@NotNull String key, Instant timestamp, E event, String logLine, Object customPayload) {
    this.partitionKey = Objects.requireNonNull(key);
    this.timestamp = timestamp;
    this.event = event;
    this.logLine = logLine;
    this.customPayload = customPayload;
  }

  /**
   * Gets the partitionKey.
   *
   * @return partitionKey.
   */
  public String getPartitionKey() {
    return partitionKey;
  }

  /**
   * Sets the partitionKey.
   *
   * @param partitionKey is partitionKey of the PObserveEvent object.
   */
  public void setPartitionKey(@NotNull String partitionKey) {
    this.partitionKey = Objects.requireNonNull(partitionKey);
  }

  /**
   * Gets the timestamp.
   *
   * @return timestamp.
   */
  public Instant getTimestamp() {
    return timestamp;
  }

  /**
   * Sets the timestamp.
   *
   * @param timestamp is timestamp of the PObserveEvent object.
   */
  public void setTimestamp(Long timestamp) {
    this.timestamp = Instant.ofEpochMilli(timestamp);
  }

  public void setTimestamp(Instant timestamp) {
    this.timestamp = timestamp;
  }

  /**
   * Gets the event wrapped.
   *
   * @return wrapped event.
   */
  public E getEvent() {
    return event;
  }

  /**
   * Sets the event wrapped.
   *
   * @param event is wrapped event used by the user.
   */
  public void setEvent(E event) {
    this.event = event;
  }

  /**
   * Computes and returns the partition of the partitionKey. The parameter partition is an integer, and it
   * indicates the number of partitions. The partition is decided by taking the modulo of partitionKey's hash
   * code.
   *
   * @param partition is the number of partitions.
   * @return partition of the partitionKey of the PObserveEvent object.
   */
  public String getKeyPartition(Integer partition) {
    if (partition == null) {
      return partitionKey;
    }
    Integer bucket = partitionKey.hashCode() % partition;
    return bucket.toString();
  }

  public String getLogLine() {
    return logLine;
  }

  public void setLogLine(String logLine) {
    this.logLine = logLine;
  }

  public Object getCustomPayload() {
    return customPayload;
  }

  public void setCustomPayload(Object customPayload) {
    this.customPayload = customPayload;
  }

  @Override
  public int compareTo(PObserveEvent o) {
    return this.timestamp.compareTo(o.getTimestamp());
  }

  @Override
  public int hashCode() {
    return Objects.hash(partitionKey, timestamp, event, customPayload, logLine);
  }

  @Override
  public boolean equals(Object obj) {
    if (this == obj) {
      return true;
    }
    if (obj == null || getClass() != obj.getClass()) {
      return false;
    }

    PObserveEvent<?> that = (PObserveEvent<?>) obj;

    return this.partitionKey.equals(that.partitionKey)
        && Objects.equals(this.timestamp, that.timestamp)
        && Objects.equals(this.customPayload, that.customPayload)
        && Objects.equals(this.logLine, that.logLine)
        && this.event.equals(that.event);
  }

  private static DateTimeFormatter formatter =
          new DateTimeFormatterBuilder().parseCaseInsensitive().appendInstant(9).toFormatter();


  @Override
  public String toString() {
    return "PObserveEvent{"
            + "partitionKey='" + partitionKey + '\''
            + ", timestamp=" + formatter.format(timestamp)
            + ", event=" + event
            + ", logLine='" + logLine + '\''
            + ", customPayload=" + customPayload
            + '}';
  }

  public String toPartialString() {
    return "PObserveEvent{"
            + "partitionKey='" + partitionKey + '\''
            + ", timestamp=" + formatter.format(timestamp)
            + ", event=" + event
            + ", customPayload=" + customPayload
            + '}';
  }
}
