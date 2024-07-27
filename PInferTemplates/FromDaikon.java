import java.util.stream.Collectors;

public class FromDaikon {
    private Map<Set<Integer>, List<Main.RawPredicate>> termsToPredicates;
    private List<Main.RawTerm> terms;
    private String templateHeaderCar;
    private String templateHeaderCdr;
    private int numExists;
    private static final String[] QUANTIFIERS = { %QUANTIFIERS% };
    private static final String[] FILTERED_INVS = { "!= null", ".getClass().getName()" };
    private static final Map<String, String> substs = new HashMap<>();

    public FromDaikon(Map<Set<Integer>, List<Main.RawPredicate>> termsToPredicates,
                      List<Main.RawTerm> terms, String templateFamily, int numExtQuantfiers) {
        this.termsToPredicates = termsToPredicates;
        this.terms = terms;
        this.templateHeaderCar = "";
        this.templateHeaderCdr = "";
        this.numExists = numExtQuantfiers;
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
                    sb.append("∃e").append(i)
                            .append(":").append(QUANTIFIERS[i]).append(i == QUANTIFIERS.length - 1 ? "." : ", ");
                }
                templateHeaderCdr = sb.toString();
                break;
            default:
                throw new IllegalArgumentException("Unknown template family: " + templateFamily);
        }
        substs.put(".toString", "");
        substs.put("one of", "∈");
        substs.put(".getPayload()", "");
        substs.put("[] elements", "");
    }

    public String getFormulaHeader(String guards, String filters) {
        return this.templateHeaderCar + runSubst(guards) + " -> " + this.templateHeaderCdr + runSubst(filters);
    }

    public String convertOutput(String line, List<Main.RawTerm> forallTerms, List<Main.RawTerm> existsTerms) {
        if (!checkValidity(line, terms)) {
            return null;
        }
        boolean didSth = false;
        for (int i = 0; i < forallTerms.size(); ++i) {
            if (!didSth && line.contains("f" + i)) {
              didSth = true;
            }
            line = line.replace("f" + i, forallTerms.get(i).shortRepr());
        }
        for (int i = 0; i < existsTerms.size(); ++i) {
            String fieldPHName = "f" + (i + forallTerms.size());
            if (!didSth && line.contains(fieldPHName)) {
              didSth = true;
            }
            line = line.replace(fieldPHName, existsTerms.get(i).shortRepr());
        }
        if (!didSth) return null;
        return runSubst(line);
    }

    private String runSubst(String line) {
        for (var subst : substs.entrySet()) {
            line = line.replace(subst.getKey(), subst.getValue());
        }
        return line;
    }

    private boolean checkValidity(String line, List<Main.RawTerm> terms) {
        for (var stub : FILTERED_INVS) {
            if (line.contains(stub)) return false;
        }
        if (line.contains("_num_e_exists_")) {
            // check # exists is related not only existentially quantified events
            for (int i = 0; i < QUANTIFIERS.length - numExists; ++i) {
                if (line.contains("f" + i)) return true;
            }
            return false;
        }
        return true;
    }
}
