public class TermEnumerator implements Iterator<List<Integer>> {
    private final List<Set<Integer>> availableCombinations;
    private List<Integer> result;
    private final int numTerms;
    private final int numChoose;
    private int i;

    public TermEnumerator(Map<Set<Integer>, List<Main.RawPredicate>> termsToPredicates, int numTotalTerms, int numChoose) {
        this.availableCombinations = new ArrayList<>(termsToPredicates.keySet());
        this.result = new ArrayList<>();
        this.numTerms = numTotalTerms;
        this.numChoose = numChoose;
        i = -1;
    }

    private void resume() {
        i++;
        if (numChoose == 1) {
            if (i < numTerms) {
                result = List.of(i);
            }
        } else {
            while (i < availableCombinations.size() && availableCombinations.get(i).size() != numChoose) {
                i++;
            }
            if (i < availableCombinations.size()) {
                result = new ArrayList<>(availableCombinations.get(i));
            }
        }
    }

    public void reset() {
        i = -1;
    }

    @Override
    public boolean hasNext() {
        resume();
        if (numChoose == 1) {
            return i < numTerms;
        } else {
            return i < availableCombinations.size();
        }
    }

    @Override
    public List<Integer> next() {
        return result;
    }
}