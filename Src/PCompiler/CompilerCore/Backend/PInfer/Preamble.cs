using System;
using System.IO;
using System.Reflection;
using Plang.Compiler.Backend.Java;

namespace Plang.Compiler.Backend.PInfer
{
    internal class PreambleConstants
    {
        internal static string MinerConfigFileName = "MinerConfig.java";
        internal static string MainFileName = "Main.java";
        internal static string TemplatesFileName = "Templates.java";
        internal static string TraceParserFileName = "TraceParser.java";
        internal static string DriverCodeFileName = "PInferDriver.java";
        internal static string FromDaikonFileName = "FromDaikon.java";
        internal static string TaskPoolFileName = "TaskPool.java";
        internal static string PredicateEnumeratorFileName = "PredicateEnumerator.java";
        internal static string TermEnumeratorFileName = "TermEnumerator.java";
        internal static string TraceReaderTemplate = @$"
import com.alibaba.fastjson2.*;

public class TraceParser {{
    private JSONArray read(File jsonFile) {{
        try (
                FileInputStream fr = new FileInputStream(jsonFile);
        ) {{
            return JSON.parseArray(fr.readAllBytes());
        }} catch (FileNotFoundException e) {{
            System.out.println(jsonFile.getAbsolutePath() + "" not found!"");
            return null;
        }} catch (IOException e) {{
            throw new RuntimeException(e);
        }}
    }}

    public List<List<{Constants.PEventsClass}<?>>> loadTrace(String fp) {{
        File jsonFile = new File(fp);
        if (!jsonFile.exists()) {{
            throw new RuntimeException(""Trace file not found: "" + fp);
        }}
        List<List<{Constants.PEventsClass}<?>>> result = new ArrayList<>();
        JSONArray traces = read(jsonFile);
        if (traces == null)
            throw new RuntimeException(""Trace "" + jsonFile.getAbsolutePath() + "" cannot be parsed"");
        for (Object obj : traces) {{
            JSONArray trace = (JSONArray) obj;
            List<{Constants.PEventsClass}<?>> events = new ArrayList<>();
            for (int i = 0; i < trace.size(); i++) {{
                JSONObject e = trace.getJSONObject(i);
                String sender = e.containsKey(""sender"") ? e.getString(""sender"") : null;
                String target = e.containsKey(""target"") ? e.getString(""target"") : null;
                String name = e.getString(""event"");
                switch (name) {{
                    %EVENT_INITIALIZATION%
                    default:
                        throw new RuntimeException(""Unspecified event type: "" + name + "". Wrong trace?"");
                }}
            }}
            result.add(events);
        }}
        return result;
    }}
}}
";

        internal static string DriverCodeTemplate = @"
public class PInferDriver {

    private static void instantiateTemplate(String templateName,
                               List<%PROJECT_NAME%.PredicateWrapper> guards,
                               List<%PROJECT_NAME%.PredicateWrapper> filters,
                               List<String> forallTerms,
                               List<String> existsTerms,
                               String[] schedules) {
        TraceParser parser = new TraceParser();
        for (String sch: schedules) {
            List<List<%EVENT_BASE%>> eventsTrace = parser.loadTrace(sch);
            switch (templateName) {
                %TEMPLATE%
                default:
                    System.err.println(""Unknown template: "" + templateName);
                    return;
            }
        }
    }

    public static void main(String[] args) {
        String[] traceFiles = args[0].split(""\\*"");
        String templateName = args[1];
        List<%PROJECT_NAME%.PredicateWrapper> guards = Arrays.stream(args[2].split(""@@"")).filter(x -> !x.isBlank()).map(x -> new %PROJECT_NAME%.PredicateWrapper(x, false)).toList();
        List<%PROJECT_NAME%.PredicateWrapper> filters = Arrays.stream(args[3].split(""@@"")).filter(x -> !x.isBlank()).map(x -> new %PROJECT_NAME%.PredicateWrapper(x, false)).toList();
        List<String> forallTerms = Arrays.stream(args[4].split(""@@"")).filter(x -> !x.isBlank()).toList();
        List<String> existsTerms = Arrays.stream(args[5].split(""@@"")).filter(x -> !x.isBlank()).toList();

        instantiateTemplate(templateName, guards, filters, forallTerms, existsTerms, traceFiles);
    }
}
";

        internal static string MainProg = ReadTemplate(MainFileName);
        internal static string FromDaikonProg = ReadTemplate(FromDaikonFileName);
        internal static string MinerConfigProg = ReadTemplate(MinerConfigFileName);
        internal static string TaskPoolProg = ReadTemplate(TaskPoolFileName);
        internal static string PredicateEnumeratorProg = ReadTemplate(PredicateEnumeratorFileName);
        internal static string TermEnumeratorProg = ReadTemplate(TermEnumeratorFileName);

        internal static string ReadTemplate(string filename)
        {
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo info = new(currentDir);
            return string.Join("\n", File.ReadAllLines(
                Path.Combine(Path.Combine(info.Parent.Parent.Parent.Parent.Parent.ToString(), "PInferTemplates"), filename)
            ));
        }
    }
}