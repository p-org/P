using System;
using System.IO;
using System.Reflection;
using Plang.Compiler.Backend.Java;

namespace Plang.Compiler.Backend.PInfer
{
    internal class PreambleConstants
    {
        internal static string CheckEventTypeFunName = "checkEventType";
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
                        break;
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

    private static void invoke(String templateName,
                               List<%PROJECT_NAME%.PredicateWrapper> predicates,
                               List<%PROJECT_NAME%.PredicateWrapper> existentialFilters,
                               List<String> terms,
                               List<List<%EVENT_BASE%>> traces) {
        switch (templateName) {
            %TEMPLATE%
            default:
                System.err.println(""Unknown template: "" + templateName);
        }
    }

    public static void main(String[] args) {
        String[] traceFiles = args[0].split(""\\*"");
        String templateName = args[1];
        List<%PROJECT_NAME%.PredicateWrapper> guards = Arrays.stream(args[2].split(""@@"")).filter(x -> !x.isBlank()).map(x -> new %PROJECT_NAME%.PredicateWrapper(x, false)).toList();
        List<%PROJECT_NAME%.PredicateWrapper> existentialFilters = Arrays.stream(args[3].split(""@@"")).filter(x -> !x.isBlank()).map(x -> new %PROJECT_NAME%.PredicateWrapper(x, false)).toList();
        List<String> terms = Arrays.stream(args[4].split(""@@"")).filter(x -> !x.isBlank()).toList();

        TraceParser parser = new TraceParser();
        List<List<%EVENT_BASE%>> traces = new ArrayList<>();
        for (String fp : traceFiles) {
            traces.addAll(parser.loadTrace(fp));
        }
        invoke(templateName, guards, existentialFilters, terms, traces);
    }
}
";

        internal static string MainProg = ReadTemplate("Main.java");
        internal static string FromDaikonProg = ReadTemplate("FromDaikon.java");

        internal static string ReadTemplate(string filename)
        {
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo info = new(currentDir);
            return string.Join("\n", File.ReadAllLines(
                Path.Combine(Path.Combine(info.Parent.Parent.Parent.Parent.Parent.ToString(), "PInferTemplates"), filename)
            ));
        }

        public static string CheckEventType(string varname, string eventType)
        {
            return $"{CheckEventTypeFunName}({varname}, \"{eventType}\")";
        }
    }
}