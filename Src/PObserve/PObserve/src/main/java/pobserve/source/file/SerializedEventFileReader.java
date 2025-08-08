package pobserve.source.file;

import pobserve.commons.PObserveEvent;
import pobserve.logger.PObserveLogger;
import pobserve.utils.SerializationUtils;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.nio.ByteBuffer;
import java.util.stream.Stream;

public class SerializedEventFileReader {
    public static Stream<PObserveEvent> readEventsFromFile(final File file) {
        Stream.Builder<PObserveEvent> streamBuilder = Stream.builder();
        FileInputStream fileInputStream =  null;
        try {
            fileInputStream = new FileInputStream(file);
            while (fileInputStream.available() > 0) {
                byte[] lengthBytes = new byte[4];
                fileInputStream.read(lengthBytes);
                int length = ByteBuffer.wrap(lengthBytes).getInt();

                byte[] serializedEventBytes = new byte[length];
                fileInputStream.read(serializedEventBytes);
                PObserveEvent event = SerializationUtils.deserializePObserveEvent(serializedEventBytes);
                streamBuilder.add(event);
            }
        } catch (Exception e) {
            PObserveLogger.error("Exception occurred while reading PObserve events from file (" + file.getAbsolutePath() + ")");
            PObserveLogger.error(e.getMessage());
        } finally {
            try {
                if (fileInputStream != null) {
                    fileInputStream.close();
                }
            } catch (IOException e) {
                PObserveLogger.error("Exception occurred while closing file stream (" + file.getAbsolutePath() + ")");
            }
        }
        return streamBuilder.build();
    }
}
