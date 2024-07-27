import java.util.stream.Collectors;
import com.alibaba.fastjson2.*;
import %PROJECT_NAME%.pinfer.FromDaikon;

public class Main {
    public static final int QUANTIFIED_EVENTS = %QUANTIFIED_EVENTS%;

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
            for (int i : predicate.terms) {
                boundedEvents.addAll(terms.get(i).events());
            }
        }
        for (int i : chosenTerms) {
            boundedEvents.addAll(terms.get(i).events());
        }
        return boundedEvents.size() == QUANTIFIED_EVENTS;
    }

    private static void specMiningForallOrExists(PredicateEnumerator enumerator,
                                                 List<RawTerm> terms,
                                                 Map<Set<Integer>, List<RawPredicate>> termsToPredicates,
                                                 MinerConfig minerConfig) throws IOException, InterruptedException {
        boolean isForall = minerConfig.templateCategory.equals("Forall");
        TermEnumerator termEnumerator = new TermEnumerator(termsToPredicates, terms.size(), minerConfig.numTermsToChoose);
        Map<String, Set<TaskPool.Task>> properties = new HashMap<>();
        List<String> propertyKeys = new ArrayList<>();
        int numTasks = 0;
        FromDaikon converter = new FromDaikon(termsToPredicates, terms, isForall ? "Forall" : "Exists", 0);
        TaskPool taskPool = new TaskPool(Runtime.getRuntime().availableProcessors(), converter, minerConfig.verbose);
        while (enumerator.hasNext()) {
            List<RawPredicate> predicateComb = enumerator.next();
            String key = predicateComb.stream().map(RawPredicate::shortRepr).collect(Collectors.joining(" && "));
            properties.put(key, new HashSet<>());
            propertyKeys.add(key);
            while (termEnumerator.hasNext()) {
                List<Integer> termComb = termEnumerator.next();
                var chosenTerms = new HashSet<>(termComb);
                boolean trivial = false;
                if (minerConfig.checkTrivialCombinations) {
                    trivial = isTrivial(predicateComb, chosenTerms)
                            || !checkQuantifierCover(predicateComb, terms, chosenTerms);
                }
                if (!trivial) {
                    numTasks += 1;
                    List<RawPredicate> guards = isForall ? predicateComb : List.of();
                    List<RawPredicate> filters = isForall ? List.of() : predicateComb;
                    List<RawTerm> forallTerms = isForall ? termComb.stream().map(terms::get).toList() : List.of();
                    List<RawTerm> existsTerms = isForall ? List.of() : termComb.stream().map(terms::get).toList();
                    var task = new TaskPool.Task(minerConfig.traces,
                            minerConfig.numForallQuantifiers,
                            minerConfig.numExistsQuantifiers,
                            guards,
                            filters,
                            forallTerms,
                            existsTerms,
                            minerConfig.templateCategory, taskPool);
                    taskPool.addTask(task);
                    properties.get(key).add(task);
                }
            }
            termEnumerator.reset();
        }
        System.out.println("Forall/Exists-only Number of tasks: " + numTasks);
        taskPool.waitForAll();
        System.out.println("Number properties mined: " + taskPool.getNumMined());
    }

    private static void specMiningForallExists(List<RawPredicate> predicateList,
                                               List<RawTerm> terms,
                                               Map<Set<Integer>, List<RawPredicate>> termsToPredicates,
                                               MinerConfig minerConfig) throws IOException, InterruptedException {
        List<RawPredicate> guardCandidates = new ArrayList<>();
        List<RawPredicate> filterCandidates = new ArrayList<>();
        Set<Integer> existsQuantifiedEvents = new HashSet<>();
        for (int i = QUANTIFIED_EVENTS - 1; i >= QUANTIFIED_EVENTS - minerConfig.numExistsQuantifiers; --i) {
            existsQuantifiedEvents.add(i);
        }
        for (RawPredicate p : predicateList) {
            var boundedEvents = p.terms().stream().map(x -> terms.get(x).events()).flatMap(Collection::stream).collect(Collectors.toSet());
            if (Collections.disjoint(boundedEvents, existsQuantifiedEvents)) {
                guardCandidates.add(p);
            } else {
                filterCandidates.add(p);
            }
        }
        PredicateEnumerator guardEnumerator = new PredicateEnumerator(minerConfig.numGuardConjunctions, guardCandidates, minerConfig.mustIncludeGuards);
        TermEnumerator termEnumerator = new TermEnumerator(termsToPredicates, terms.size(), minerConfig.numTermsToChoose);
        Map<String, Map<String, List<TaskPool.Task>>> tasks = new HashMap<>();
        int numTasks = 0;
        FromDaikon converter = new FromDaikon(termsToPredicates, terms, "ForallExists", minerConfig.numExistsQuantifiers);
        TaskPool taskPool = new TaskPool(Runtime.getRuntime().availableProcessors(), converter, minerConfig.verbose);
        Map<String, List<String>> keysSequences = new HashMap<>();
        List<String> guardKeySequence = new ArrayList<>();
        while (guardEnumerator.hasNext()) {
            var guards = guardEnumerator.next();
            var guardsKey = guards.stream().map(RawPredicate::shortRepr).collect(Collectors.joining(" && "));
            tasks.put(guardsKey, new HashMap<>());
            keysSequences.put(guardsKey, new ArrayList<>());
            guardKeySequence.add(guardsKey);
            PredicateEnumerator filterEnumerator = new PredicateEnumerator(minerConfig.numFilterConjunctions, filterCandidates, minerConfig.mustIncludeFilters);
            while (filterEnumerator.hasNext()) {
                var filters = filterEnumerator.next();
                var filtersKey = filters.stream().map(RawPredicate::shortRepr).collect(Collectors.joining(" && "));
                tasks.get(guardsKey).put(filtersKey, new ArrayList<>());
                keysSequences.get(guardsKey).add(filtersKey);
                while (termEnumerator.hasNext()) {
                    var termComb = termEnumerator.next();
                    Set<Integer> existsQuantifiedTerms = termComb.stream()
                            .filter(x -> !Collections.disjoint(terms.get(x).events(), existsQuantifiedEvents))
                            .collect(Collectors.toSet());
                    if (existsQuantifiedTerms.isEmpty() && filters.isEmpty()) continue;
                    List<Integer> arrangedTerms = new ArrayList<>(termComb.stream()
                            .filter(x -> !existsQuantifiedTerms.contains(x)).toList());
                    List<Integer> forallQuantifiedTerms = new ArrayList<>(arrangedTerms);
                    arrangedTerms.addAll(existsQuantifiedTerms);
                    List<RawPredicate> chosenPredicates = new ArrayList<>(guards);
                    chosenPredicates.addAll(filters);
                    if (minerConfig.checkTrivialCombinations) {
                        Set<Integer> chosenTermsSet = new HashSet<>(arrangedTerms);
                        if (!checkQuantifierCover(chosenPredicates, terms, chosenTermsSet)
                                || isTrivial(chosenPredicates, chosenTermsSet)) {
                            continue;
                        }
                    }
                    var task = new TaskPool.Task(minerConfig.traces,
                            minerConfig.numForallQuantifiers,
                            minerConfig.numExistsQuantifiers,
                            guards,
                            filters,
                            forallQuantifiedTerms.stream().map(terms::get).toList(),
                            existsQuantifiedTerms.stream().map(terms::get).toList(),
                            minerConfig.templateCategory,
                            taskPool);
                    tasks.get(guardsKey).get(filtersKey).add(task);
                    taskPool.addTask(task);
                    numTasks += 1;
                }
                termEnumerator.reset();
            }
        }
        System.out.println("Forall-Exists Number of tasks: " + numTasks);
        taskPool.waitForAll();
        System.out.println("Number properties mined: " + taskPool.getNumMined());
    }

    public static void main(String[] args) throws Exception {
        MinerConfig minerConfig = MinerConfig.fromCommandLineArgs(args);
        List<RawPredicate> predicateList = new ArrayList<>();
        try {
            Map<Set<Integer>, List<RawPredicate>> termsToPredicates = getTermsToPredicates(new FileInputStream(minerConfig.atomicPredicatesPath), predicateList);
            List<RawTerm> terms = getTerms(new FileInputStream(minerConfig.termsPath));
            switch (minerConfig.templateCategory) {
                case "Forall" ->
                        specMiningForallOrExists(new PredicateEnumerator(minerConfig.numGuardConjunctions,
                                    predicateList,
                                    minerConfig.mustIncludeGuards),
                                terms,
                                termsToPredicates,
                                minerConfig);
                case "Exists" ->
                        specMiningForallOrExists(new PredicateEnumerator(minerConfig.numFilterConjunctions,
                                        predicateList,
                                        minerConfig.mustIncludeFilters),
                                terms,
                                termsToPredicates,
                                minerConfig);
                case "ForallExists" ->
                        specMiningForallExists(
                            predicateList,
                            terms,
                            termsToPredicates,
                            minerConfig);
            }
        } catch (InterruptedException e) {
            System.exit(1);
        } catch (IOException e) {
            System.out.println("IOException: " + e.getMessage());
        }
    }
}
