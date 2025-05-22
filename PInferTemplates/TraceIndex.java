public class TraceIndex {
    Map<List<prt.events.PEvent<?>>, Map<Class<?>, List<Integer>>> indexedTraces;
    private static final List<Integer> EMPTY = new ArrayList<>();

    public TraceIndex() {
        indexedTraces = new HashMap<>();
    }

    public Map<Class<?>, List<Integer>> getTraceIndex(List<prt.events.PEvent<?>> trace) {
        buildIndex(trace);
        return indexedTraces.get(trace);
    }

    public List<Integer> getIndices(List<prt.events.PEvent<?>> trace, Class<?> cls) {
        buildIndex(trace);
        return indexedTraces.get(trace).getOrDefault(cls, EMPTY);
    }

    public boolean indexed(List<prt.events.PEvent<?>> trace, Class<?> cls) {
        return !getIndices(trace, cls).isEmpty();
    }

    public void buildIndex(List<prt.events.PEvent<?>> trace) {
        if (!indexedTraces.containsKey(trace)) {
            Map<Class<?>, List<Integer>> index = new HashMap<>();
            int i = 0;
            for (var e: trace) {
                if (!index.containsKey(e.getClass())) {
                    index.put(e.getClass(), new ArrayList<>());
                }
                index.get(e.getClass()).add(i);
                i += 1;
            }
            indexedTraces.put(trace, index);
        }
    }
}