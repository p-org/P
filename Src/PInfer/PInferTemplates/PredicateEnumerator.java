public class PredicateEnumerator implements Iterator<List<Main.RawPredicate>> {

    private int depth;
    private final int maxDepth;
    private final List<Main.RawPredicate> predicates;
    private final Map<Integer, Integer> currentContradictions;
    private final Map<Integer, Integer> execPtr;
    private final Map<Integer, Integer> programLoc;
    private List<Main.RawPredicate> currentCombination;
    private Set<Integer> currentCombinationOrders;
    private boolean finished = false;

    private static final int LOOP_HEAD = 1;
    private static final int LOOP_BODY = 1 << 1;
    private static final int LOOP_CALL = 1 << 2;
    private static final int RETURN = 1 << 3;

    private static final int YIELD = 1 << 4;
    private static final int FINISHED = 1 << 5;
    private static final int CONTINUE = 1 << 6;

    public PredicateEnumerator(int maxDepth, List<Main.RawPredicate> predicates, Set<Integer> mustInclude) {
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
        for (Main.RawPredicate predicate : currentCombination) {
            if (currentContradictions.containsKey(predicate.order())) {
                throw new RuntimeException("Axiom set inconsistent: "
                        + predicate.shortRepr() + " (" + predicate.order() + ") marked as contradiction");
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

    private void pushPredicate(Main.RawPredicate predicate) {
        // equivalent to set currentCombination[depth] to `predicate`
        assert currentCombination.size() == depth: "Cannot push at level " + depth
                + " where the work list is at depth " + currentCombination.size();
        currentCombination.add(predicate);
        currentCombinationOrders.add(predicate.order());
        addContradictions(predicate.contradictions());
    }

    private void popPredicate() {
        // equivalent to removing currentCombination[depth]
        assert !currentCombination.isEmpty();
        assert depth == currentCombination.size() - 1: "Cannot pop at level " + depth
                + " while the work list is at level " + (currentCombination.size() - 1);
        Main.RawPredicate last = currentCombination.removeLast();
        currentCombinationOrders.remove(last.order());
        removeContradiction(last.contradictions());
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
                        Main.RawPredicate pred = predicates.get(ptr);
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
    public List<Main.RawPredicate> next() {
        return new ArrayList<>(currentCombination);
    }
}