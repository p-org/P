package pobserve.utils;

import pobserve.commons.PObserveEvent;
import pobserve.runtime.Monitor;

import org.nustaq.serialization.FSTConfiguration;

/**
 * Utility class for serialization and deserialization of objects.
 */
public class SerializationUtils {

    /**
     * ThreadLocal to keep FST Configuration per thread.
     */
    private static final ThreadLocal<FSTConfiguration> FST_INSTANCES =
            ThreadLocal.withInitial(FSTConfiguration::createDefaultConfiguration);

    /**
     * Deserialize a PObserveEvent from byte array using FST deserialization.
     *
     * @param bytes Serialized PObserveEvent
     * @return Deserialized PObserveEvent
     */
    public static PObserveEvent deserializePObserveEvent(byte[] bytes) {
        FSTConfiguration fst = FST_INSTANCES.get();
        PObserveEvent event = (PObserveEvent) fst.asObject(bytes);
        return event;
    }

    /**
     * Serialize a PObserveEvent to a byte array using FST serialization.
     *
     * @param event PObserve event to serialize
     * @return Serialized pobserve event
     */
    public static byte[] serializePObserveEvent(PObserveEvent event) {
        FSTConfiguration fst = FST_INSTANCES.get();
        return fst.asByteArray(event);
    }

    /**
     * Serialize a pobserve monitor to a byte array using FST serialization.
     *
     * @param monitor PObserve monitor to serialize
     * @return Serialized pobserve monitor
     */
    public static byte[] serializeMonitor(Monitor<?> monitor) {
        FSTConfiguration fst = FST_INSTANCES.get();
        return fst.asByteArray(monitor);
    }

    /**
     * Deserialize a pobserve monitor from byte array using FST deserialization.
     *
     * @param bytes Serialized pobserve monitor
     * @return Deserialized pobserve monitor
     */
    public static Monitor<?> deserializeMonitor(byte[] bytes) {
        FSTConfiguration fst = FST_INSTANCES.get();

        // Try to deserialize using FST's asObject
        Monitor<?> monitor = (Monitor<?>) fst.asObject(bytes);
        monitor.reInitializeMonitor();
        return monitor;
    }

    /**
     * Create a deep copy of a Monitor by using serialization/deserialization.
     *
     * @param monitor The Monitor to clone
     * @return A deep copy of the Monitor
     */
    public static Monitor<?> cloneMonitor(Monitor<?> monitor) {
        Monitor<?> cloned = deserializeMonitor(serializeMonitor(monitor));
        return cloned;
    }
}
