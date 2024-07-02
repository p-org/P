namespace Plang.Compiler.Backend.PInfer
{
    internal class PreambleConstants
    {
        internal static string CheckEventTypeFunName = "checkEventType";
        internal static string TraceReaderTemplate = @"
import com.alibaba.fastjson2.*;

public class TraceParser {
    private final File jsonFile;
    private final Pattern MACHINE_NAME_PATTERN = Pattern.compile(""\\(([0-9]+)\\)$"");

    private long parseMachineId(String name) {
        Matcher m = MACHINE_NAME_PATTERN.matcher(name);
        if (m.find()) {
            return Long.parseLong(m.group(1));
        } else {
            throw new RuntimeException(name + "" is not a valid machine name"");
        }
    }

    private JSONArray read() {
        try (
                FileInputStream fr = new FileInputStream(jsonFile);
        ) {
            return JSON.parseArray(fr.readAllBytes());
        } catch (FileNotFoundException e) {
            System.out.println(jsonFile.getAbsolutePath() + "" not found!"");
            return null;
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }

    public TraceParser(String filePath) {
        jsonFile = new File(filePath);
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
                    eventPayload.put(""sender"", parseMachineId(details.getString(""sender"")));
                    eventPayload.put(""target"", parseMachineId(details.getString(""target"")));
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

        internal static string DriverCodeTemplate = @"
public class PInferDriver {
    private static String getTypeName(String termRepr) {
        String[] s = termRepr.split(""=>"");
        return s[1].strip().split("" "")[0];
    }

    private static void invoke(int numQuantifier, List<String> termTypes,
                               List<TwoPhaseCommit.PredicateWrapper> predicates, List<String> terms,
                               List<List<PEvents.EventBase>> traces) {
        String templateName = ""Forall"" + numQuantifier + ""Events"" + String.join("""", termTypes);
        switch (templateName) {
            %TEMPLATE%
            default:
                throw new RuntimeException(""Unknown template: "" + templateName);
        }
    }

    private static Set<String> getQuantifiedEvents(String repr) {
        String quantifiers = repr.split(""where"")[1].strip();
        Pattern p = Pattern.compile(""\\((e[0-9]+):\\w+\\)"");
        Matcher m = p.matcher(quantifiers);
        Set<String> quantifiedEvents = new HashSet<>();
        while (m.find()) {
            quantifiedEvents.add(m.group(1));
        }
        return quantifiedEvents;
    }

    public static void main(String[] args) {
        String tracePath = args[0];
        List<TwoPhaseCommit.PredicateWrapper> guards = new ArrayList<>();
        List<String> terms = new ArrayList<>();
        Set<String> quantifiedEvents = new HashSet<>();
        int i;
        for (i = 1; !args[i].equals(""-t""); ++i) {
            guards.add(new TwoPhaseCommit.PredicateWrapper(args[i], false));
            quantifiedEvents.addAll(getQuantifiedEvents(args[i]));
        }
        i += 1;
        while (i < args.length) {
            terms.add(args[i]);
            quantifiedEvents.addAll(getQuantifiedEvents(args[i]));
            i += 1;
        }

        TraceParser parser = new TraceParser(tracePath);
        var traces = parser.loadTrace();
        List<String> termTypes = terms.stream().map(PInferDriver::getTypeName).toList();
        invoke(quantifiedEvents.size(), termTypes, guards, terms, traces);
    }
}
";

        public static string CheckEventType(string varname, string eventType)
        {
            return $"{CheckEventTypeFunName}({varname}, \"{eventType}\")";
        }
    }
}