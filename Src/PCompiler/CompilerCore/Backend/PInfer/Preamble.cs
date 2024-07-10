namespace Plang.Compiler.Backend.PInfer
{
    internal class PreambleConstants
    {
        internal static string CheckEventTypeFunName = "checkEventType";
        internal static string TraceReaderTemplate = @"
import com.alibaba.fastjson2.*;

public class TraceParser {
    private JSONArray read(File jsonFile) {
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

    public List<List<PEvents.EventBase>> loadTrace(String fp) {
        File jsonFile = new File(fp);
        if (!jsonFile.exists()) {
            throw new RuntimeException(""Trace file not found: "" + fp);
        }
        List<List<PEvents.EventBase>> result = new ArrayList<>();
        JSONArray traces = read(jsonFile);
        if (traces == null)
            throw new RuntimeException(""Trace "" + jsonFile.getAbsolutePath() + "" cannot be parsed"");
        for (Object obj : traces) {
            JSONArray trace = (JSONArray) obj;
            List<PEvents.EventBase> events = new ArrayList<>();
            for (int i = 0; i < trace.size(); i++) {
                JSONObject e = trace.getJSONObject(i);
                if (e.getString((""type"")).equals(""SendEvent"") || e.getString((""type"")).equals(""Announce"")) {
                    JSONObject details = e.getJSONObject(""details"");
                    JSONObject eventPayload = new JSONObject();
                    eventPayload.put(""sender"", details.getString(""sender""));
                    eventPayload.put(""target"", details.getString(""target""));
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

    private static void invoke(String templateName,
                               List<%PROJECT_NAME%.PredicateWrapper> predicates,
                               List<%PROJECT_NAME%.PredicateWrapper> existentialFilters,
                               List<String> terms,
                               List<List<PEvents.EventBase>> traces) {
        switch (templateName) {
            %TEMPLATE%
            default:
                throw new RuntimeException(""Unknown template: "" + templateName);
        }
    }

    public static void main(String[] args) {
        String[] traceFiles = args[0].split(""\\*"");
        String templateName = args[1];
        List<%PROJECT_NAME%.PredicateWrapper> guards = Arrays.stream(args[2].split(""@@"")).filter(x -> !x.isBlank()).map(x -> new %PROJECT_NAME%.PredicateWrapper(x, false)).toList();
        List<%PROJECT_NAME%.PredicateWrapper> existentialFilters = Arrays.stream(args[3].split(""@@"")).filter(x -> !x.isBlank()).map(x -> new %PROJECT_NAME%.PredicateWrapper(x, false)).toList();
        List<String> terms = Arrays.stream(args[4].split(""@@"")).filter(x -> !x.isBlank()).toList();

        TraceParser parser = new TraceParser();
        List<List<PEvents.EventBase>> traces = new ArrayList<>();
        for (String fp : traceFiles) {
            traces.addAll(parser.loadTrace(fp));
        }
        invoke(templateName, guards, existentialFilters, terms, traces);
    }
}
";

        internal static string MainProg = @"import java.io.*;
import java.util.*;
import java.util.stream.Collectors;
import com.alibaba.fastjson2.*;

public class Main {
    private static final int QUANTIFIED_EVENTS = %QUANTIFIED_EVENTS%;

    public static class RawPredicate {
        private int order;
        private String repr;
        private String shortRepr;
        private Set<Integer> contradictions;
        private Set<Integer> terms;

        public RawPredicate(int order, String repr, int[] contradictions, int[] terms) {
            this.order = order;
            this.repr = repr;
            this.shortRepr = repr.split(""=>"")[0].strip();
            this.contradictions = Arrays.stream(contradictions).boxed().collect(Collectors.toSet());
            this.terms = Arrays.stream(terms).boxed().collect(Collectors.toSet());
        }

        public int order() {
            return order;
        }

        public String repr() {
            return repr;
        }

        public Set<Integer> contradictions() {
            return contradictions;
        }

        public Set<Integer> terms() {
            return terms;
        }
    }

    public record RawTerm(String repr, String type, Set<Integer> events) {
        public String shortRepr() {
            return repr.split(""=>"")[0].strip();
        }
    }

    private static Set<Integer> toIntSet(String line) {
        if (line == null || line.isBlank()) return Collections.emptySet();
        return Arrays.stream(line.split("" ""))
                .mapToInt(Integer::parseInt)
                .boxed()
                .collect(Collectors.toSet());
    }

    private static class PredicateCombinationEnumerator implements Iterator<List<RawPredicate>> {

        private int depth;
        private final int maxDepth;
        private final List<RawPredicate> predicates;
        private final Map<Integer, Integer> currentContradictions;
        private final List<RawPredicate> currentCombination;
        private final Map<Integer, Integer> execPtr;
        private final Map<Integer, Integer> programLoc;
        private boolean finished = false;

        private static final int LOOP_HEAD = 1;
        private static final int LOOP_BODY = 1 << 1;
        private static final int LOOP_CALL = 1 << 2;
        private static final int RETURN = 1 << 3;

        private static final int YIELD = 1 << 4;
        private static final int FINISHED = 1 << 5;
        private static final int CONTINUE = 1 << 6;

        public PredicateCombinationEnumerator(int maxDepth, List<RawPredicate> predicates) {
            this.maxDepth = maxDepth;
            this.predicates = predicates;
            this.depth = 0;
            this.currentContradictions = new HashMap<>();
            this.currentCombination = new ArrayList<>();
            this.programLoc = new HashMap<>();
            this.execPtr = new HashMap<>();
            for (int i = 0; i <= maxDepth; i++) {
                // break symmetry
                this.execPtr.put(i, i);
                this.programLoc.put(i, LOOP_HEAD);
            }
        }

        private void addContradictions(Set<Integer> contradictions) {
            for (Integer contradiction : contradictions) {
                int n = currentContradictions.getOrDefault(contradiction, 0);
                currentContradictions.put(contradiction, n + 1);
            }
        }

        private void removeContradiction(Set<Integer> contradictions) {
            for (Integer contradiction : contradictions) {
                if (currentContradictions.containsKey(contradiction)) {
                    int n = currentContradictions.get(contradiction);
                    if (n - 1 == 0)
                        currentContradictions.remove(contradiction);
                    else
                        currentContradictions.put(contradiction, n - 1);
                }
            }
        }

        private void pushPredicate(RawPredicate predicate) {
            // equivalent to set currentCombination[depth] to `predicate`
            assert currentCombination.size() == depth: ""Cannot push at level "" + depth
                    + "" where the work list is at depth "" + currentCombination.size();
            currentCombination.add(predicate);
            addContradictions(predicate.contradictions);
        }

        private void popPredicate() {
            // equivalent to removing currentCombination[depth]
            assert !currentCombination.isEmpty();
            assert depth == currentCombination.size() - 1: ""Cannot pop at level "" + depth
                    + "" while the work list is at level "" + (currentCombination.size() - 1);
            RawPredicate last = currentCombination.removeLast();
            removeContradiction(last.contradictions);
        }

        private String showLoc(int loc) {
            return switch (loc) {
                case LOOP_HEAD -> ""LOOP_HEAD"";
                case LOOP_BODY -> ""LOOP_BODY"";
                case LOOP_CALL -> ""LOOP_CALL"";
                case RETURN -> ""RETURN"";
                case YIELD -> ""YIELD"";
                case FINISHED -> ""FINISHED"";
                case CONTINUE -> ""CONTINUE"";
                default -> ""UNKNOWN "" + loc;
            };
        }

        private void goTo(int loc) {
            programLoc.put(depth, loc);
        }

        private int step() {
            if (finished || depth < 0)
                return FINISHED;
            int loc = programLoc.get(depth);
            if (depth == 0 && loc == RETURN) {
                finished = true;
                return FINISHED;
            }
            // execute current ptr
            int status = CONTINUE;
            while (status == CONTINUE) {
                loc = programLoc.get(depth);
                switch (loc) {
                    case LOOP_HEAD -> {
                        if (depth == maxDepth) {
                            depth -= 1;
                            status = YIELD;
                        } else {
                            goTo(LOOP_BODY);
                        }
                    }
                    case LOOP_BODY -> {
                        int ptr = execPtr.get(depth);
                        while (ptr < predicates.size() && currentContradictions.containsKey(ptr)) {
                            ptr += 1;
                        }
                        if (ptr >= predicates.size()) {
                            // exit loop
                            goTo(RETURN);
                        } else {
                            execPtr.put(depth, ptr);
                            RawPredicate pred = predicates.get(ptr);
                            pushPredicate(pred);
                            goTo(LOOP_CALL);
                            depth += 1;
                            execPtr.put(depth, ptr + 1);
                        }
                    }
                    case LOOP_CALL -> {
                        // returned from the next depth
                        popPredicate();
                        execPtr.put(depth, execPtr.get(depth) + 1);
                        goTo(LOOP_BODY);
                    }
                    case RETURN -> {
                        goTo(LOOP_HEAD);
                        execPtr.put(depth, depth); // reset pt
                        if (depth == 0) {
                            goTo(FINISHED);
                            status = FINISHED;
                        }
                        depth -= 1;
                    }
                    default -> throw new IllegalStateException(""Unexpected program location: "" + loc);
                }
            }
            return status;
        }

        private void resume() {
            int loc;
            while (true) {
                loc = step();
                if (loc == FINISHED) {
                    finished = true;
                    return;
                }
                if (loc == YIELD) return;
            }
        }

        @Override
        public boolean hasNext() {
            // seek to the next valid execution point
            resume();
            return !finished;
        }

        @Override
        public List<RawPredicate> next() {
            return new ArrayList<>(currentCombination);
        }
    }

    private record Tuple<T>(T car, T cdr) {}

    private static class TermTupleEnumerator implements Iterator<Tuple<Integer>> {
        private final Map<Set<Integer>, List<RawPredicate>> termsToPredicates;
        private final List<Tuple<Integer>> memo;
        private Tuple<Integer> result;
        private int i, j, p;
        private boolean memoized = false;
        private final int numTerms;

        public TermTupleEnumerator(Map<Set<Integer>, List<RawPredicate>> termsToPredicates, int numTerms) {
            this.termsToPredicates = termsToPredicates;
            this.result = new Tuple<>(null, null);
            memo = new ArrayList<>();
            i = 0;
            j = i + 1;
            p = 0;
            this.numTerms = numTerms;
        }

        private boolean step() {
            while (j < numTerms) {
                if (termsToPredicates.containsKey(Set.of(i, j))) {
                    result = new Tuple<>(i, j);
                    memo.add(result);
                    j += 1;
                    return true;
                }
                j += 1;
            }
            return false;
        }

        private void resume() {
            if (memoized) {
                return;
            }
            while (!step() && i < numTerms) {
                i += 1;
                j = i + 1;
            }
        }

        public void reset() {
            assert memoized;
            p = 0;
        }

        @Override
        public boolean hasNext() {
            resume();
            if (!memoized) {
                boolean r = i < numTerms;
                if (!r) {
                    memoized = true;
                    p = memo.size();
                }
                return r;
            }
            return p < memo.size();
        }

        @Override
        public Tuple<Integer> next() {
            if (memoized) {
                Tuple<Integer> result = memo.get(p);
                p += 1;
                return result;
            }
            return result;
        }
    }

    private static class Task {

        private final List<String> tracePaths;
        private final List<RawPredicate> predicates;
        private final List<RawTerm> terms;
        private final List<RawPredicate> existentialFilter;
        private String templateName;
        private final StringBuilder daikonOutput;
        private final StringBuilder daikonStdErr;
        private Process runningProg;
        private Thread outputThread;
        private boolean forallExists;

        public Task(List<String> tracePaths, List<RawPredicate> predicates, List<RawTerm> terms, List<RawPredicate> existentialFilter, boolean forallExists) {
            this.tracePaths = tracePaths;
            this.forallExists = forallExists;
            this.predicates = predicates;
            this.terms = terms.stream().sorted(Comparator.comparing(t -> t.type)).toList();
            this.existentialFilter = existentialFilter;
            this.daikonOutput = new StringBuilder();
            this.daikonStdErr = new StringBuilder();
            this.runningProg = null;
            this.outputThread = null;
        }

        public Set<String> getDaikonOutput(FromDaikon converter) throws InterruptedException {
            assert this.outputThread != null;
            this.outputThread.join();
            String result = daikonOutput.toString();
            String prop;
            String[] lines = result.split(""\n"");
            Set<String> properties = new HashSet<>();
            boolean start = false;
            boolean hasResult = false;
            for (String line : lines) {
                if (line.contains("":::ENTER"") && line.contains(templateName + ""."" + templateName)) {
                    start = true;
                    hasResult = true;
                    continue;
                }
                if (line.contains(""====="") && start) break;
                if (start && (prop = converter.convertOutput(line, predicates, existentialFilter, terms)) != null) {
                    properties.add(prop);
                }
            }
//            System.out.println(""StdErr: "" + daikonStdErr);
            if (!hasResult && daikonStdErr.toString().contains(""No program point declarations were found."")) {
                return null;
            }
            return properties;
        }

        public String showTask() {
            StringBuilder builder = new StringBuilder();
            builder.append(""Template: "").append(templateName).append(""\n"");
            builder.append(""Predicates: "").append(predicates.stream().map(x -> x.repr).collect(Collectors.joining("" ""))).append(""\n"");
            builder.append(""Existential filters: "").append(existentialFilter.stream().map(x -> x.repr).collect(Collectors.joining("" ""))).append(""\n"");
            builder.append(""Terms: "").append(terms.stream().map(RawTerm::shortRepr).collect(Collectors.joining("" ""))).append(""\n"");
            return builder.toString();
        }

        private void watch() {
            outputThread = new Thread(() -> {
                BufferedReader reader = new BufferedReader(new InputStreamReader(runningProg.getInputStream()));
                String line;
                while (true) {
                    try {
                        if ((line = reader.readLine()) == null) {
                            break;
                        }
                        daikonOutput.append(line).append(""\n"");
                    } catch (IOException e) {
                        throw new RuntimeException(e);
                    }
                }
                reader = new BufferedReader(new InputStreamReader(runningProg.getErrorStream()));
                while (true) {
                    try {
                        if ((line = reader.readLine()) == null) {
                            break;
                        }
                        daikonStdErr.append(line).append(""\n"");
                    } catch (IOException e) {
                        throw new RuntimeException(e);
                    }
                }
            });
            outputThread.start();
        }

        public void start() throws IOException {
            StringBuilder templateNameBuilder;
            if (existentialFilter.isEmpty()) {
                templateNameBuilder = new StringBuilder(""Forall"");
            } else if (!forallExists) {
                templateNameBuilder = new StringBuilder(""Exists"");
            }else {
                templateNameBuilder = new StringBuilder(""ForallExists"");
            }
            templateNameBuilder.append(QUANTIFIED_EVENTS).append(""Events"");
            for (var t: terms) {
                templateNameBuilder.append(t.type);
            }
            this.templateName = templateNameBuilder.toString().strip();
            ProcessBuilder pb = new ProcessBuilder(""java"",
                    ""-Xmx32g"",
                    ""-cp"",
                    System.getProperty(""java.class.path""),
                    ""daikon.Chicory"",
                    ""--ppt-select-pattern="" + ""Templates"",
                    ""--ppt-omit-pattern=execute"",
                    ""--dtrace-file="" + Integer.toHexString(showTask().hashCode()) + "".dtrace.gz"",
                    ""--daikon"",
                    ""%PROJECT_NAME%.pinfer.PInferDriver"",
                    String.join(""*"", tracePaths),
                    templateName,
                    predicates.stream().map(x -> x.repr).collect(Collectors.joining(""@@"")),
                    existentialFilter.stream().map(x -> x.repr).collect(Collectors.joining(""@@"")),
                    terms.stream().map(x -> x.repr).collect(Collectors.joining(""@@"")));
//            System.out.println(String.join("" "", pb.command()));
            runningProg = pb.start();
            watch();
        }
    }

    private static Map<Set<Integer>, List<RawPredicate>>
    getTermsToPredicates(FileInputStream fp, List<RawPredicate> predicateList) throws IOException {
        HashMap<Set<Integer>, List<RawPredicate>> termsToPredicates = new HashMap<>();
        JSONArray predicateMetadata = JSON.parseArray(fp.readAllBytes());
        for (int i = 0; i < predicateMetadata.size(); i++) {
            RawPredicate p = predicateMetadata.getObject(i, RawPredicate.class);
            predicateList.add(p);
            var predList = termsToPredicates.getOrDefault(p.terms(), new ArrayList<>());
            predList.add(p);
            termsToPredicates.put(p.terms(), predList);
        }
        return termsToPredicates;
    }

    private static List<RawTerm> getTerms(FileInputStream fp) throws IOException {
        List<RawTerm> terms = new ArrayList<>();
        JSONArray termsMetadata = JSON.parseArray(fp.readAllBytes());
        for (int i = 0; i < termsMetadata.size(); i++) {
            RawTerm t = termsMetadata.getObject(i, RawTerm.class);
            terms.add(t);
        }
        return terms;
    }

    private static void showTemplateInput(List<RawPredicate> guards, List<String> terms) {
        String guardStr = guards.stream().map(x -> x.repr().split(""=>"")[0]).collect(Collectors.joining("" && ""));
        String termStr = terms.stream().map(x -> x.split(""=>"")[0]).collect(Collectors.joining("", ""));
        System.out.println(guardStr + "" | "" + termStr);
    }

    private static boolean isTrivial(List<RawPredicate> predicates, Set<Integer> terms) {
        for (RawPredicate predicate : predicates) {
            if (predicate.terms.equals(terms)) return true;
        }

        return false;
    }

    private static boolean checkQuantifierCover(List<RawPredicate> predicates, List<RawTerm> terms, Set<Integer> chosenTerms) {
        Set<Integer> boundedEvents = new HashSet<>();
        for (RawPredicate predicate : predicates) {
            for (int i: predicate.terms) {
                boundedEvents.addAll(terms.get(i).events());
            }
        }
        for (int i : chosenTerms) {
            boundedEvents.addAll(terms.get(i).events());
        }
        return boundedEvents.size() == QUANTIFIED_EVENTS;
    }

    private static void specMiningForall(PredicateCombinationEnumerator enumerator, List<RawTerm> terms,
                                         Map<Set<Integer>, List<RawPredicate>> termsToPredicates,
                                         List<String> tracePath,
                                         boolean trivialityCheck) throws IOException, InterruptedException {
        TermTupleEnumerator termTupleEnumerator = new TermTupleEnumerator(termsToPredicates, terms.size());
        Map<String, Set<Task>> properties = new HashMap<>();
        int numTasks = 0;
        while (enumerator.hasNext()) {
            List<RawPredicate> comb = enumerator.next();
            String key = comb.stream().map(x -> x.shortRepr).collect(Collectors.joining("" && ""));
            properties.put(key, new HashSet<>());
            // 1 field
            for (int i = 0; i < terms.size(); ++i) {
                Set<Integer> setOfE = Set.of(i);
                if (!termsToPredicates.containsKey(setOfE) || !checkQuantifierCover(comb, terms, setOfE)) {
                    continue;
                }
                boolean trivial = false;
                if (trivialityCheck) {
                    trivial = isTrivial(comb, setOfE);
                }
                if (!trivial) {
                    numTasks += 1;
                    var task = new Task(tracePath, comb, List.of(terms.get(i)), List.of(), false);
                    properties.get(key).add(task);
                }
            }
            // 2 fields
            while (termTupleEnumerator.hasNext()) {
                Tuple<Integer> termTuple = termTupleEnumerator.next();
                var chosenTerms = Set.of(termTuple.car(), termTuple.cdr());
                if (!checkQuantifierCover(comb, terms, chosenTerms)) {
                    continue;
                }
                boolean trivial = false;
                if (trivialityCheck) {
                    trivial = isTrivial(comb, chosenTerms);
                }
                if (!trivial) {
                    numTasks += 1;
                    var task = new Task(tracePath, comb, List.of(terms.get(termTuple.car()), terms.get(termTuple.cdr())), List.of(), false);
                    properties.get(key).add(task);
                }
            }
            termTupleEnumerator.reset();
        }
        System.out.println(""Forall-only Number of tasks: "" + numTasks);
        int numSolved = 0;
        FromDaikon converter = new FromDaikon(termsToPredicates, terms);
        for (var guards: properties.keySet()) {
            Set<Task> tasks = properties.get(guards);
            if (!tasks.isEmpty()) {
                System.out.println(""========================"" + numSolved + ""/"" + numTasks + ""=================================="");
                System.out.println(""Predicates: "" + guards);
                var iter = tasks.iterator();
                Task t = iter.next();
                iter.remove();
                t.start();
                Set<String> invariants = t.getDaikonOutput(converter);
                numSolved += 1;
                if (invariants != null) {
                    System.out.println(""Properties:"");
                    while (iter.hasNext()) {
                        iter.next().start();
                    }
                    for (Task task: tasks) {
                        var result = task.getDaikonOutput(converter);
                        if (result != null) {
                            invariants.addAll(result);
                        }
                        numSolved += 1;
                    }
                    System.out.println(String.join(""\n"", invariants));
                    System.out.println(""=========================================================="");
                } else {
                    System.out.println(""Vacuous after guards filter, skipped "" + tasks.size());
                    numSolved += tasks.size();
                }
            }
        }
    }

    private static void specMiningForallExists(int guardDepth, List<RawPredicate> predicateList, List<RawTerm> terms, String tracePath) throws IOException, InterruptedException {

    }

    public static void main(String[] args) {
        String predicatePath = args[0];
        String termsPath = args[1];
        int predicateDepth = Integer.parseInt(args[2]);
        int checkTrivial = Integer.parseInt(args[3]);
        List<String> traceFiles = new ArrayList<>(Arrays.asList(args).subList(4, args.length - 1));
        List<RawPredicate> predicateList = new ArrayList<>();
        try {
            Map<Set<Integer>, List<RawPredicate>> termsToPredicates = getTermsToPredicates(new FileInputStream(predicatePath), predicateList);
            List<RawTerm> terms = getTerms(new FileInputStream(termsPath));
            if (args[args.length - 1].equals(""--forall""))
                specMiningForall(new PredicateCombinationEnumerator(predicateDepth, predicateList), terms,
                        termsToPredicates, traceFiles, checkTrivial == 1);
        } catch (InterruptedException e) {
            System.exit(1);
        } catch (IOException e) {
            System.out.println(""IOException: "" + e.getMessage());
        }
    }
}";

        public static string CheckEventType(string varname, string eventType)
        {
            return $"{CheckEventTypeFunName}({varname}, \"{eventType}\")";
        }
    }
}