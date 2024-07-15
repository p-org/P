import java.util.stream.Collectors;

public class FromDaikon {
    private Map<Set<Integer>, List<Main.RawPredicate>> termsToPredicates;
    private List<Main.RawTerm> terms;
    public String templateHeader;
    private static final String[] QUANTIFIERS = { %QUANTIFIERS% };
    private static final String[] FILTERED_INVS = { "!= null", ".getClass().getName()" };
    private static final Map<String, String> substs = new HashMap<>();

    public FromDaikon(Map<Set<Integer>, List<Main.RawPredicate>> termsToPredicates,
                      List<Main.RawTerm> terms, String templateFamily) {
        this.termsToPredicates = termsToPredicates;
        this.terms = terms;
        StringBuilder sb = new StringBuilder();
        switch (templateFamily.toLowerCase()) {
            case "forall":
                for (int i = 0; i < QUANTIFIERS.length; ++i) {
                    sb.append("∀e").append(i)
                            .append(": ").append(QUANTIFIERS[i]).append(i == QUANTIFIERS.length - 1 ? ". " : ", ");
                }
                break;
            case "exists":
                for (int i = 0; i < QUANTIFIERS.length; ++i) {
                    sb.append("∃e").append(i)
                            .append(":").append(QUANTIFIERS[i]).append(i == QUANTIFIERS.length - 1 ? ". " : ", ");
                }
                break;
            case "forall-exists":
                for (int i = 0; i < QUANTIFIERS.length - 1; ++i) {
                    sb.append("∀e").append(i)
                            .append(":").append(QUANTIFIERS[i]).append(i == QUANTIFIERS.length - 1 ? ". " : ", ");
                }
                sb.append("∃e").append(QUANTIFIERS.length - 1)
                        .append(":").append(QUANTIFIERS[QUANTIFIERS.length - 1]).append(". ");
            default:
                throw new IllegalArgumentException("Unknown template family: " + templateFamily);
        }
        templateHeader = sb.toString();
        substs.put("toString", "");
        substs.put("one of", "∈");
    }

    public String convertOutput(String line, List<Main.RawPredicate> predicates, List<Main.RawPredicate> filters, List<Main.RawTerm> terms) {
        if (!checkValidity(line, terms)) {
            return null;
        }
        for (int i = 0; i < terms.size(); i++) {
            line = line.replace("f" + i, terms.get(i).shortRepr());
        }
        for (var subst : substs.entrySet()) {
            line = line.replace(subst.getKey(), subst.getValue());
        }
        return line;
    }

    private boolean checkValidity(String line, List<Main.RawTerm> terms) {
        for (var stub : FILTERED_INVS) {
            if (line.contains(stub)) return false;
        }
        return true;
    }
}
