import java.util.stream.Collectors;

public class FromDaikon {
    private Map<Set<Integer>, List<Main.RawPredicate>> termsToPredicates;
    private List<Main.RawTerm> terms;
    private String templateHeaderCar;
    private String templateHeaderCdr;
    private static final String[] QUANTIFIERS = { %QUANTIFIERS% };
    private static final String[] FILTERED_INVS = { "!= null", ".getClass().getName()" };
    private static final Map<String, String> substs = new HashMap<>();

    public FromDaikon(Map<Set<Integer>, List<Main.RawPredicate>> termsToPredicates,
                      List<Main.RawTerm> terms, String templateFamily, int numExtQuantfiers) {
        this.termsToPredicates = termsToPredicates;
        this.terms = terms;
        this.templateHeaderCar = "";
        this.templateHeaderCdr = "";
        StringBuilder sb = new StringBuilder();
        switch (templateFamily) {
            case "Forall":
                for (int i = 0; i < QUANTIFIERS.length; ++i) {
                    sb.append("∀e").append(i)
                            .append(": ").append(QUANTIFIERS[i]).append(i == QUANTIFIERS.length - 1 ? ". " : ", ");
                }
                templateHeaderCar = sb.toString();
                break;
            case "Exists":
                for (int i = 0; i < QUANTIFIERS.length; ++i) {
                    sb.append("∃e").append(i)
                            .append(":").append(QUANTIFIERS[i]).append(i == QUANTIFIERS.length - 1 ? ". " : ", ");
                }
                templateHeaderCar = sb.toString();
                break;
            case "ForallExists":
                for (int i = 0; i < QUANTIFIERS.length - numExtQuantfiers; ++i) {
                    sb.append("∀e").append(i)
                            .append(":").append(QUANTIFIERS[i]).append(i == QUANTIFIERS.length - numExtQuantfiers - 1 ? "." : ", ");
                }
                templateHeaderCar = sb.toString();
                sb = new StringBuilder();
                for (int i = QUANTIFIERS.length - numExtQuantfiers; i < QUANTIFIERS.length; ++i) {
                    sb.append("∃e").append(QUANTIFIERS.length - 1)
                            .append(":").append(i == QUANTIFIERS.length - 1 ? "." : ", ");
                }
                templateHeaderCdr = sb.toString();
                break;
            default:
                throw new IllegalArgumentException("Unknown template family: " + templateFamily);
        }
        substs.put(".toString", "");
        substs.put("one of", "∈");
    }

    public String getFormulaHeader(String guards, String filters) {
        return this.templateHeaderCar + guards + " -> " + this.templateHeaderCdr + filters;
    }

    public String convertOutput(String line, List<Main.RawTerm> forallTerms, List<Main.RawTerm> existsTerms) {
        if (!checkValidity(line, terms)) {
            return null;
        }
        for (int i = 0; i < forallTerms.size(); ++i) {
            line = line.replace("f" + i, forallTerms.get(i).shortRepr());
        }
        for (int i = 0; i < existsTerms.size(); ++i) {
            line = line.replace("f" + (i + forallTerms.size()), existsTerms.get(i).shortRepr());
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
