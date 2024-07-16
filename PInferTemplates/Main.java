import java.util.stream.Collectors;
import com.alibaba.fastjson2.*;
import org.apache.commons.cli.*;
import %PROJECT_NAME%.pinfer.FromDaikon;

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
            this.shortRepr = repr.split("=>")[0].strip();
            this.contradictions = Arrays.stream(contradictions).boxed().collect(Collectors.toSet());
            this.terms = Arrays.stream(terms).boxed().collect(Collectors.toSet());
        }

        public int order() {
            return order;
        }

        public String repr() {
            return repr;
        }

        public String shortRepr() { 
            return shortRepr;
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
            return repr.split("=>")[0].strip().replace(".getPayload()", "");
        }
    }

    private static class PredicateCombinationEnumerator implements Iterator<List<RawPredicate>> {

        private int depth;
        private final int maxDepth;
        private final List<RawPredicate> predicates;
        private final Map<Integer, Integer> currentContradictions;
        private final Map<Integer, Integer> execPtr;
        private final Map<Integer, Integer> programLoc;
        private List<RawPredicate> currentCombination;
        private Set<Integer> currentCombinationOrders;
        private boolean finished = false;

        private static final int LOOP_HEAD = 1;
        private static final int LOOP_BODY = 1 << 1;
        private static final int LOOP_CALL = 1 << 2;
        private static final int RETURN = 1 << 3;

        private static final int YIELD = 1 << 4;
        private static final int FINISHED = 1 << 5;
        private static final int CONTINUE = 1 << 6;

        public PredicateCombinationEnumerator(int maxDepth, List<RawPredicate> predicates, Set<Integer> mustInclude) {
            this.maxDepth = maxDepth;
            this.predicates = predicates;
            this.depth = 0;
            this.currentContradictions = new HashMap<>();
            for (int i = 0; i < predicates.size(); ++i) {
                if (mustInclude.contains(predicates.get(i).order())) {
                    for (Integer contradiction : predicates.get(i).contradictions()) {
                        currentContradictions.put(contradiction, i);
                    }
                }
            }
            this.currentCombination = new ArrayList<>(predicates.stream().filter(x -> mustInclude.contains(x.order())).toList());
            this.currentCombinationOrders = new HashSet<>();
            for (RawPredicate predicate : currentCombination) {
                if (currentContradictions.containsKey(predicate.order())) {
                    throw new RuntimeException("Axiom set inconsistent: "
                            + predicate.shortRepr + " (" + predicate.order() + ") marked as contradiction");
                }
                this.currentCombinationOrders.add(predicate.order());
            }
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
            assert currentCombination.size() == depth: "Cannot push at level " + depth
                    + " where the work list is at depth " + currentCombination.size();
            currentCombination.add(predicate);
            currentCombinationOrders.add(predicate.order());
            addContradictions(predicate.contradictions);
        }

        private void popPredicate() {
            // equivalent to removing currentCombination[depth]
            assert !currentCombination.isEmpty();
            assert depth == currentCombination.size() - 1: "Cannot pop at level " + depth
                    + " while the work list is at level " + (currentCombination.size() - 1);
            RawPredicate last = currentCombination.removeLast();
            currentCombinationOrders.remove(last.order());
            removeContradiction(last.contradictions);
        }

        private String showLoc(int loc) {
            return switch (loc) {
                case LOOP_HEAD -> "LOOP_HEAD";
                case LOOP_BODY -> "LOOP_BODY";
                case LOOP_CALL -> "LOOP_CALL";
                case RETURN -> "RETURN";
                case YIELD -> "YIELD";
                case FINISHED -> "FINISHED";
                case CONTINUE -> "CONTINUE";
                default -> "UNKNOWN " + loc;
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
                        while (ptr < predicates.size()
                                && (currentContradictions.containsKey(predicates.get(ptr).order())
                                || currentCombinationOrders.contains(predicates.get(ptr).order()))) {
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
                    default -> throw new IllegalStateException("Unexpected program location: " + loc);
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

        private static enum Type {
            FORALL, EXISTS, FORALLEXISTS
        }
        private final List<String> tracePaths;
        private final List<RawPredicate> predicates;
        private final List<RawTerm> terms;
        private final List<RawPredicate> existentialFilter;
        private String templateName;
        private final StringBuilder daikonOutput;
        private final StringBuilder daikonStdErr;
        private Process runningProg;
        private Thread outputThread;
        private Type taskType;

        public Task(List<String> tracePaths, List<RawPredicate> predicates, List<RawTerm> terms, List<RawPredicate> existentialFilter, Type taskType) {
            this.tracePaths = tracePaths;
            this.taskType = taskType;
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
            String[] lines = result.split("\n");
            Set<String> properties = new HashSet<>();
            boolean start = false;
            boolean hasResult = false;
            for (String line : lines) {
                if (line.contains(":::ENTER") && line.contains(templateName + "." + templateName)) {
                    start = true;
                    hasResult = true;
                    continue;
                }
                if (line.contains("=====") && start) break;
                if (start && (prop = converter.convertOutput(line, predicates, existentialFilter, terms)) != null) {
                    properties.add(prop);
                }
            }
            var stderr = daikonStdErr.toString().trim();
            if (stderr.contains("Exception")) {
                System.out.println("Exception raised: " + stderr);
                return properties;
            }
            if (stderr.contains("Unknown template")) {
                System.out.println("Skipping unknown template: " + stderr);
                return properties;
            }
            if (!hasResult && daikonStdErr.toString().contains("No program point declarations were found.")) {
                return null;
            }
            return properties;
        }

        public String showTask() {
            StringBuilder builder = new StringBuilder();
            builder.append("Template: ").append(templateName).append("\n");
            builder.append("Predicates: ").append(predicates.stream().map(x -> x.repr).collect(Collectors.joining(" "))).append("\n");
            builder.append("Existential filters: ").append(existentialFilter.stream().map(x -> x.repr).collect(Collectors.joining(" "))).append("\n");
            builder.append("Terms: ").append(terms.stream().map(RawTerm::shortRepr).collect(Collectors.joining(" "))).append("\n");
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
                        daikonOutput.append(line).append("\n");
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
                        daikonStdErr.append(line).append("\n");
                    } catch (IOException e) {
                        throw new RuntimeException(e);
                    }
                }
            });
            outputThread.start();
        }

        public void start() throws IOException {
            StringBuilder templateNameBuilder;
            if (taskType == Type.FORALL) {
                templateNameBuilder = new StringBuilder("Forall");
            } else if (taskType == Type.EXISTS) {
                templateNameBuilder = new StringBuilder("Exists");
            } else {
                templateNameBuilder = new StringBuilder("ForallExists");
            }
            templateNameBuilder.append(QUANTIFIED_EVENTS).append("Events");
            for (var t: terms) {
                templateNameBuilder.append(t.type);
            }
            this.templateName = templateNameBuilder.toString().strip();
            ProcessBuilder pb = new ProcessBuilder("java",
                    "-Xmx32g",
                    "-cp",
                    System.getProperty("java.class.path"),
                    "daikon.Chicory",
                    "--ppt-select-pattern=" + "Templates",
                    "--ppt-omit-pattern=execute",
                    "--dtrace-file=" + Integer.toHexString(showTask().hashCode()) + ".dtrace.gz",
                    "--daikon",
                    "%PROJECT_NAME%.pinfer.PInferDriver",
                    String.join("*", tracePaths),
                    templateName,
                    predicates.stream().map(x -> x.repr).collect(Collectors.joining("@@")),
                    existentialFilter.stream().map(x -> x.repr).collect(Collectors.joining("@@")),
                    terms.stream().map(x -> x.repr).collect(Collectors.joining("@@")));
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
        String guardStr = guards.stream().map(x -> x.repr().split("=>")[0]).collect(Collectors.joining(" && "));
        String termStr = terms.stream().map(x -> x.split("=>")[0]).collect(Collectors.joining(", "));
        System.out.println(guardStr + " | " + termStr);
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

    private static void specMiningForallOrExists(PredicateCombinationEnumerator enumerator, List<RawTerm> terms,
                                         Map<Set<Integer>, List<RawPredicate>> termsToPredicates,
                                         List<String> tracePath,
                                         boolean isForall,
                                         boolean trivialityCheck) throws IOException, InterruptedException {
        TermTupleEnumerator termTupleEnumerator = new TermTupleEnumerator(termsToPredicates, terms.size());
        Map<String, Set<Task>> properties = new HashMap<>();
        int numTasks = 0;
        while (enumerator.hasNext()) {
            List<RawPredicate> comb = enumerator.next();
            String key = comb.stream().map(x -> x.shortRepr).collect(Collectors.joining(" && "));
            properties.put(key, new HashSet<>());
            // 1 field
            for (int i = 0; i < terms.size(); ++i) {
                Set<Integer> setOfE = Set.of(i);
                boolean trivial = false;
                if (trivialityCheck) {
                    trivial = isTrivial(comb, setOfE) || !termsToPredicates.containsKey(setOfE) || !checkQuantifierCover(comb, terms, setOfE);
                }
                if (!trivial) {
                    numTasks += 1;
                    List<RawPredicate> guards = isForall ? comb : List.of();
                    List<RawPredicate> existentialFilter = isForall ? List.of() : comb;
                    var task = new Task(tracePath, guards, List.of(terms.get(i)), existentialFilter, isForall ? Task.Type.FORALL : Task.Type.EXISTS);
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
                    List<RawPredicate> guards = isForall ? comb : List.of();
                    List<RawPredicate> existentialFilter = isForall ? List.of() : comb;
                    var task = new Task(tracePath, guards, List.of(terms.get(termTuple.car()), terms.get(termTuple.cdr())), existentialFilter, isForall ? Task.Type.FORALL : Task.Type.EXISTS);
                    properties.get(key).add(task);
                }
            }
            termTupleEnumerator.reset();
        }
        System.out.println("Forall/Exists-only Number of tasks: " + numTasks);
        int numSolved = 0;
        FromDaikon converter = new FromDaikon(termsToPredicates, terms, isForall ? "forall" : "exists");
        for (var guards: properties.keySet()) {
            Set<Task> tasks = properties.get(guards);
            if (!tasks.isEmpty()) {
                System.out.println("========================" + numSolved + "/" + numTasks + "==================================");
                System.out.println(converter.getFormulaHeader(guards, ""));
                var iter = tasks.iterator();
                Task t = iter.next();
                iter.remove();
                t.start();
                Set<String> invariants = t.getDaikonOutput(converter);
                numSolved += 1;
                if (invariants != null) {
                    // System.out.println("Properties:");
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
                    System.out.println(String.join("\n", invariants));
                    System.out.println("==========================================================");
                } else {
                    System.out.println("Vacuous after guards filter, skipped " + tasks.size());
                    numSolved += tasks.size();
                }
            }
        }
    }

    private static void specMiningForallExists(int guardDepth, int filterDepth,
                                               List<RawPredicate> predicateList,
                                               List<RawTerm> terms,
                                               Map<Set<Integer>, List<RawPredicate>> termsToPredicates,
                                               Set<Integer> guardsMustInclude,
                                               Set<Integer> filterMustInclude,
                                               List<String> tracePath,
                                               boolean trivialityCheck) throws IOException, InterruptedException {
        List<RawPredicate> guardCandidates = new ArrayList<>();
        List<RawPredicate> filterCandidates = new ArrayList<>();
        for (RawPredicate p : predicateList) {
            var boundedEvents = p.terms().stream().map(x -> terms.get(x).events()).flatMap(Collection::stream).collect(Collectors.toSet());
            if (!boundedEvents.contains(QUANTIFIED_EVENTS - 1)) {
                guardCandidates.add(p);
            } else {
                filterCandidates.add(p);
            }
        }
        PredicateCombinationEnumerator guardEnumerator = new PredicateCombinationEnumerator(guardDepth, guardCandidates, guardsMustInclude);
        PredicateCombinationEnumerator filterEnumerator = new PredicateCombinationEnumerator(filterDepth, filterCandidates, filterMustInclude);
        TermTupleEnumerator termTupleEnumerator = new TermTupleEnumerator(termsToPredicates, terms.size());
        Map<String, Map<String, List<Task>>> tasks = new HashMap<>();
        int numTasks = 0;
        while (guardEnumerator.hasNext()) {
            var guards = guardEnumerator.next();
            var guardsKey = guards.stream().map(RawPredicate::shortRepr).collect(Collectors.joining(" && "));
            tasks.put(guardsKey, new HashMap<>());
            while (filterEnumerator.hasNext()) {
                var filters = filterEnumerator.next();
                var filtersKey = filters.stream().map(RawPredicate::shortRepr).collect(Collectors.joining(" && "));
                tasks.get(guardsKey).put(filtersKey, new ArrayList<>());
                while (termTupleEnumerator.hasNext()) {
                    var termTuple = termTupleEnumerator.next();
                    // maintain assumption: last term only related to existentially quantified events
                    var carTerm = terms.get(termTuple.car());
                    var cdrTerm = terms.get(termTuple.cdr());
                    if (!cdrTerm.events().equals(Set.of(QUANTIFIED_EVENTS - 1))) continue;
                    List<RawPredicate> chosenPredicates = new ArrayList<>(guards);
                    var termList = List.of(carTerm, cdrTerm);
                    chosenPredicates.addAll(filters);
                    if (trivialityCheck) {
                        if (!checkQuantifierCover(chosenPredicates, terms, Set.of(termTuple.car(), termTuple.cdr()))
                                || isTrivial(chosenPredicates, Set.of(termTuple.car(), termTuple.cdr()))) {
                            continue;
                        }
                    }
                    tasks.get(guardsKey).get(filtersKey).add(
                            new Task(tracePath, guards, termList, filters, Task.Type.FORALLEXISTS)
                    );
                    numTasks += 1;
                }
                termTupleEnumerator.reset();
            }
        }
        System.out.println("Forall-Exists Number of tasks: " + numTasks);
        int numSolved = 0;
        FromDaikon converter = new FromDaikon(termsToPredicates, terms, "forall-exists");
        for (var guards: tasks.keySet()) {
            for (var filters: tasks.get(guards).keySet()) {
                var taskSet = tasks.get(guards).get(filters);
                if (!taskSet.isEmpty()) {
                    System.out.println("========================" + numSolved + "/" + numTasks + "==================================");
                    System.out.println(converter.getFormulaHeader(guards, filters));
                    for (var task: taskSet) {
                        task.start();
                    }
                    Set<String> invariants = new HashSet<>();
                    for (var task: taskSet) {
                        var result = task.getDaikonOutput(converter);
                        if (result != null) {
                            invariants.addAll(result);
                        }
                    }
                    numSolved += tasks.size();
                    System.out.println(String.join("\n", invariants));
                    System.out.println("==========================================================");
                }
            }
        }
    }

    private static Options argParserSetup() {
        Options options = new Options();
        Option predicatePathOpt = new Option("p", "predicates", true,
                "Path to predicates.json");
        predicatePathOpt.setRequired(true);
        options.addOption(predicatePathOpt);

        Option termsPathOpt = new Option("t", "terms", true,
                "Path to terms.json");
        termsPathOpt.setRequired(true);
        options.addOption(termsPathOpt);

        Option predDepthOpt = new Option("gd", "guard-depth", true,
                "Number of conjunctions in guards");
        predDepthOpt.setRequired(true);
        options.addOption(predDepthOpt);

        Option filterDepthOpt = new Option("fd", "filter-depth", true,
                "Number of conjunctions in filter (only required when template contains existential quantifications)");
        filterDepthOpt.setRequired(false);
        options.addOption(filterDepthOpt);

        Option trivialityCheck = new Option("st", "skip-trivial", false,
                "Filter out trivial combinations of predicates and terms");
        trivialityCheck.setRequired(false);
        options.addOption(trivialityCheck);

        Option mode = new Option("template", true, "forall/forall-exists/exists");
        mode.setRequired(true);
        options.addOption(mode);

        Option traceFilesOpt = new Option("l", "logs", true,
                "A list of P logs in JSON format");
        traceFilesOpt.setRequired(true);
        traceFilesOpt.setArgs(Option.UNLIMITED_VALUES);
        options.addOption(traceFilesOpt);

        Option mustIncludeOpt = new Option("g", "include-guards", true,
                "A list of predicates ids that must be included in the guards");
        mustIncludeOpt.setRequired(false);
        mustIncludeOpt.setArgs(Option.UNLIMITED_VALUES);
        options.addOption(mustIncludeOpt);

        Option mustIncludeFilterOpt = new Option("f", "include-filters", true,
                "A list of predicate ids that must be included in the existential filter");
        mustIncludeFilterOpt.setRequired(false);
        mustIncludeFilterOpt.setArgs(Option.UNLIMITED_VALUES);
        options.addOption(mustIncludeFilterOpt);

        return options;
    }

    public static CommandLine parseArgs(String[] args, Options options) {
        DefaultParser parser = new DefaultParser();
        HelpFormatter formatter = new HelpFormatter();

        try {
            return parser.parse(options, args);
        } catch (ParseException e) {
            System.err.println(e.getMessage());
            formatter.printHelp("%PROJECT_NAME%.pinfer.Main", options);
            System.exit(1);
        }
        return null;
    }

    public static void main(String[] args) throws Exception {
        Options opts = argParserSetup();
        CommandLine cmd = parseArgs(args, opts);
        String predicatePath = cmd.getOptionValue("predicates");
        String termsPath = cmd.getOptionValue("terms");
        int predicateDepth = Integer.parseInt(cmd.getOptionValue("guard-depth", "0"));
        int filterDepth = Integer.parseInt(cmd.getOptionValue("filter-depth", "0"));
        int checkTrivial = cmd.hasOption("skip-trivial") ? 1 : 0;
        List<String> traceFiles = List.of(cmd.getOptionValues("logs"));
        List<RawPredicate> predicateList = new ArrayList<>();
        try {
            Map<Set<Integer>, List<RawPredicate>> termsToPredicates = getTermsToPredicates(new FileInputStream(predicatePath), predicateList);
            List<RawTerm> terms = getTerms(new FileInputStream(termsPath));
            var opt = cmd.getOptionValue("template");
            Set<Integer> mustIncludeGuardSet = Set.of();
            Set<Integer> mustIncludeFilterSet = Set.of();
            if (cmd.hasOption("include-guards")) {
                mustIncludeGuardSet = Arrays.stream(cmd.getOptionValues("include-guards"))
                        .mapToInt(Integer::parseInt)
                        .boxed()
                        .collect(Collectors.toSet());
            }
            if (cmd.hasOption("include-filters")) {
                mustIncludeFilterSet = Arrays.stream(cmd.getOptionValues("include-filters"))
                        .mapToInt(Integer::parseInt)
                        .boxed()
                        .collect(Collectors.toSet());
            }
            switch (opt) {
                case "forall" ->
                        specMiningForallOrExists(new PredicateCombinationEnumerator(Math.max(predicateDepth - mustIncludeGuardSet.size(), 0),
                                predicateList, mustIncludeGuardSet), terms, termsToPredicates, traceFiles, true, checkTrivial == 1);
                case "exists" ->
                        specMiningForallOrExists(new PredicateCombinationEnumerator(Math.max(filterDepth - mustIncludeFilterSet.size(), 0),
                                predicateList, mustIncludeFilterSet), terms, termsToPredicates, traceFiles, false, checkTrivial == 1);
                case "forall-exists" -> specMiningForallExists(Math.max(predicateDepth - mustIncludeGuardSet.size(), 0),
                        Math.max(filterDepth - mustIncludeFilterSet.size(), 0),
                        predicateList,
                        terms, termsToPredicates, mustIncludeGuardSet, mustIncludeFilterSet, traceFiles, checkTrivial == 1);
            }
        } catch (InterruptedException e) {
            System.exit(1);
        } catch (IOException e) {
            System.out.println("IOException: " + e.getMessage());
        }
    }
}