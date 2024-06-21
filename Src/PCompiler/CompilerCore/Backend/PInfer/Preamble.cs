namespace Plang.Compiler.Backend.PInfer
{
    internal class PreambleConstants
    {
        internal static string CheckEventTypeFunName = "checkEventType";
        internal static string TraceReaderTemplate = @"
public class TraceParser {
    private final JSONPath cachePath;
    private final File jsonFile;
    private final Pattern MACHINE_NAME_PATTERN = Pattern.compile(""^[a-zA-Z0-9_-]+\\((0-9+)\\)$"");

    private long parseMachineId(String name) {
        Matcher m = MACHINE_NAME_PATTERN.matcher(name);
        if (m.find()) {
            return Long.parseLong(m.group(0));
        } else {
            throw new RuntimeException(name + "" is not a valid machine name"");
        }
    }

    private JSONArray read() {
        try (
                FileInputStream fr = new FileInputStream(jsonFile);
                JSONReader jsonReader = JSONReader.of(fr.readAllBytes())
        ) {
            return (JSONArray) cachePath.extract(jsonReader);
        } catch (FileNotFoundException e) {
            System.out.println(jsonFile.getAbsolutePath() + "" not found!"");
            return null;
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }

    public TraceParser(String filePath) {
        jsonFile = new File(filePath);
        cachePath = JSONPath.of("".$"" + jsonFile.getAbsolutePath());
    }

    public List<List<PEvents.EventBase>> loadTrace() {
        List<List<PEvents.EventBase>> result = new ArrayList<>();
        JSONArray traces = read();
        if (traces == null)
            throw new RuntimeException(""Trace "" + jsonFile.getAbsolutePath() + "" cannot be parsed"");
        for (Object obj : traces) {
            JSONArray trace = (JSONArray) obj;
            List<PEvents.EventBase> events = new ArrayList<>();
            for (int i = 0; i < trace.size(); i++) {
                JSONObject e = trace.getJSONObject(i);
                if (e.getString((""type"")).equals(""SendEvent"")) {
                    JSONObject details = e.getJSONObject(""details"");
                    JSONObject eventPayload = new JSONObject();
                    eventPayload.put(""sender"", parseMachineId(e.getString(""sender"")));
                    eventPayload.put(""target"", parseMachineId(e.getString(""target"")));
                    String name = details.getString(""event"");
                    switch (name) {
                        %EVENT_INITIALIZATION%
                        default:
                            break;
                    }
                }
            }
            result.add(events);
        }
        return result;
    }
}
";

        public static string CheckEventType(string varname, string eventType)
        {
            return $"{CheckEventTypeFunName}({varname}, \"{eventType}\")";
        }
    }
}