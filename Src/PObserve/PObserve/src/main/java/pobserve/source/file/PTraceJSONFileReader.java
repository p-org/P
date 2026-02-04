package pobserve.source.file;

import java.io.File;
import java.io.FileReader;
import java.nio.charset.StandardCharsets;
import java.util.stream.Stream;
import org.json.simple.JSONArray;
import org.json.simple.JSONObject;
import org.json.simple.parser.JSONParser;

/**
 * PTraceJSONFileReader class helps read logs from a json file
 */
public class PTraceJSONFileReader implements PObserveFileReader {

    /**
     * Reads logs from json file
     * @param file json file containing logs
     * @return stream of logs
     */
    @Override
    public Stream<Object> readFile(File file) {
        JSONParser parser = new JSONParser();
        Stream<Object> eventStream = Stream.empty();
        try (FileReader fileReader = new FileReader(file, StandardCharsets.UTF_8)) {
            JSONArray logArray = (JSONArray) parser.parse(fileReader);
            for (Object object : logArray) {
                JSONObject log = (JSONObject) object;
                String type = (String) log.get("type");
                if (type.equalsIgnoreCase("sendevent") || type.equalsIgnoreCase("announce")) {
                    eventStream = Stream.concat(eventStream, Stream.of(log));
                }
            }
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
        return eventStream;
    }
}
