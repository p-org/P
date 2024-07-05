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
        String tracePath = args[0];
        String templateName = args[1];
        List<%PROJECT_NAME%.PredicateWrapper> guards = new ArrayList<>();
        List<%PROJECT_NAME%.PredicateWrapper> existentialFilters = new ArrayList<>();
        List<String> terms = new ArrayList<>();
        assert args[2].equals(""--predicates"");
        int i;
        for (i = 3; !args[i].equals(""--filters""); ++i) {
            guards.add(new %PROJECT_NAME%.PredicateWrapper(args[i], false));
        }
        i += 1;
        while (i < args.length && !args[i].equals(""--terms"")) {
            existentialFilters.add(new %PROJECT_NAME%.PredicateWrapper(args[i], false));
            i += 1;
        }
        i += 1;
        while (i < args.length) {
            terms.add(args[i]);
            i += 1;
        }
        TraceParser parser = new TraceParser(tracePath);
        var traces = parser.loadTrace();
        invoke(templateName, guards, existentialFilters, terms, traces);
    }
}
";

        internal static string MainProg = @"import java.io.*;
import java.util.*;
import java.util.stream.Collectors;

public class Main {
    private record RawPredicate(String repr, Set<Integer> contradictions, Set<Integer> boundedTerms) {

        public boolean contradictsWith(int predicateId) {
            return this.contradictions.contains(predicateId);
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
            switch (loc) {
                case LOOP_HEAD -> {
                    if (depth == maxDepth) {
                        depth -= 1;
                        return YIELD;
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
                    }
                    depth -= 1;
                }
                default -> throw new IllegalStateException(""Unexpected program location: "" + loc);
            }
            return CONTINUE;
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
            return currentCombination;
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

    private static Map<Set<Integer>, List<RawPredicate>>
    getTermsToPredicates(BufferedReader reader, List<RawPredicate> predicateList) throws IOException {
        HashMap<Set<Integer>, List<RawPredicate>> termsToPredicates = new HashMap<>();
        String line;
        Set<Integer> boundedTerms = Set.of();
        Set<Integer> contradictions = Set.of();
        int lineNum = 0;
        while ((line = reader.readLine()) != null) {
            if (lineNum % 3 == 0) {
                // First line is the bounded terms
                boundedTerms = toIntSet(line);
            } else if (lineNum % 3 == 1) {
                // Second lines are contradictions
                contradictions = toIntSet(line);
            } else {
                // third line is `<id> <predicate>`
                RawPredicate predicate = new RawPredicate(Arrays.stream(line.split("" ""))
                        .skip(1)
                        .collect(Collectors.joining("" "")), contradictions, boundedTerms);
                List<RawPredicate> values = termsToPredicates.getOrDefault(boundedTerms, new ArrayList<>());
                values.add(predicate);
                termsToPredicates.put(boundedTerms, values);
                predicateList.add(predicate);
            }
            lineNum += 1;
        }
        return termsToPredicates;
    }

    private static List<String> getTerms(BufferedReader reader) throws IOException {
        String line;
        List<String> terms = new ArrayList<>();
        while ((line = reader.readLine()) != null) {
            terms.add(Arrays.stream(line.split("" "")).skip(1).collect(Collectors.joining("" "")));
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
            if (predicate.boundedTerms.equals(terms)) return true;
        }
        return false;
    }

    private static void predicateEnumeration(PredicateCombinationEnumerator enumerator, List<String> terms,
                                             Map<Set<Integer>, List<RawPredicate>> termsToPredicates, boolean trivialityCheck) {
        TermTupleEnumerator termTupleEnumerator = new TermTupleEnumerator(termsToPredicates, terms.size());
        int numTasks = 0;
        while (enumerator.hasNext()) {
            List<RawPredicate> comb = enumerator.next();
            // 1 field
            for (int i = 0; i < terms.size(); ++i) {
                Set<Integer> setOfE = Set.of(i);
                if (!termsToPredicates.containsKey(setOfE)) {
                    continue;
                }
                boolean trivial = false;
                if (trivialityCheck) {
                    trivial = isTrivial(comb, setOfE);
                }
                if (!trivial) {
                    showTemplateInput(comb, List.of(terms.get(i)));
                    numTasks += 1;
                }
            }
            // 2 fields
            while (termTupleEnumerator.hasNext()) {
                Tuple<Integer> termTuple = termTupleEnumerator.next();
                boolean trivial = false;
                if (trivialityCheck) {
                    trivial = isTrivial(comb, Set.of(termTuple.car(), termTuple.cdr()));
                }
                if (!trivial) {
                    showTemplateInput(comb, List.of(terms.get(termTuple.car()), terms.get(termTuple.cdr())));
                    numTasks += 1;
                }
            }
            termTupleEnumerator.reset();
        }
        System.out.println(""Number of tasks: "" + numTasks);
    }

    public static void main(String[] args) {
        String predicatePath = args[0];
        String termsPath = args[1];
        int predicateDepth = Integer.parseInt(args[2]);
        int checkTrivial = Integer.parseInt(args[3]);
        List<RawPredicate> predicateList = new ArrayList<>();
        try {
            BufferedReader predicateReader = new BufferedReader(new FileReader(predicatePath));
            BufferedReader termsReader = new BufferedReader(new FileReader(termsPath));
            Map<Set<Integer>, List<RawPredicate>> termsToPredicates = getTermsToPredicates(predicateReader, predicateList);
            List<String> terms = getTerms(termsReader);
            predicateEnumeration(new PredicateCombinationEnumerator(predicateDepth, predicateList), terms, termsToPredicates, checkTrivial == 1);
        } catch (IOException e) {
            System.exit(1);
        }
    }
}";

        public static string CheckEventType(string varname, string eventType)
        {
            return $"{CheckEventTypeFunName}({varname}, \"{eventType}\")";
        }
    }
}