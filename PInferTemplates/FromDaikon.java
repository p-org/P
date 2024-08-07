import java.util.stream.Collectors;

public class FromDaikon {
    private Map<Set<Integer>, List<Main.RawPredicate>> termsToPredicates;
    private List<Main.RawTerm> terms;
    private String templateHeaderCar;
    private String templateHeaderCdr;
    private int pruningLevel;
    private int numExists;
    private static final String[] QUANTIFIERS = { %QUANTIFIERS% };
    private static final String[] FILTERED_INVS = { "!= null", ".getClass().getName()", "[] ==" };
    private static final String[] COMP_OPS = { "!=", "<=", "<", ">=", ">" };
    private static final Map<String, String> substs = new HashMap<>();

    public FromDaikon(Map<Set<Integer>, List<Main.RawPredicate>> termsToPredicates,
                      List<Main.RawTerm> terms, String templateFamily, int numExtQuantfiers, int pruningLevel) {
        this.termsToPredicates = termsToPredicates;
        this.terms = terms;
        this.templateHeaderCar = "";
        this.templateHeaderCdr = "";
        this.numExists = numExtQuantfiers;
        this.pruningLevel = pruningLevel;
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
                            .append(":").append(QUANTIFIERS[i]).append(i == QUANTIFIERS.length - numExtQuantfiers - 1 ? " :: " : ", ");
                }
                templateHeaderCar = sb.toString();
                sb = new StringBuilder();
                for (int i = QUANTIFIERS.length - numExtQuantfiers; i < QUANTIFIERS.length; ++i) {
                    sb.append("∃e").append(i)
                            .append(":").append(QUANTIFIERS[i]).append(i == QUANTIFIERS.length - 1 ? " :: " : ", ");
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

    public String convertOutput(String line, List<Main.RawPredicate> guards, List<Main.RawPredicate> filters,
                                List<Main.RawTerm> forallTerms, List<Main.RawTerm> existsTerms) {
        if (!checkValidity(line, guards, filters, forallTerms, existsTerms)) {
            return null;
        }
        List<Main.RawTerm> substTerms = new ArrayList<>();
        for (int i = 0; i < forallTerms.size(); ++i) {
            if (line.contains("f" + i)) {
                substTerms.add(forallTerms.get(i));
                line = line.replace("f" + i, forallTerms.get(i).shortRepr());
            }
        }
        for (int i = 0; i < existsTerms.size(); ++i) {
            String fieldPHName = "f" + (i + forallTerms.size());
            if (line.contains(fieldPHName)) {
                // do not check sorted by for aggregated array
                if (line.contains("sorted by")) return null;
                substTerms.add(existsTerms.get(i));
                line = line.replace(fieldPHName, existsTerms.get(i).shortRepr());
            }
        }
        boolean didSth = !substTerms.isEmpty();
        if (!didSth && !line.contains("_num_e_exists_")) return null;
        if (line.contains("_num_e_exists_")) {
            // _num_e_exists_ should be on lhs
            if (!line.startsWith("_num_e_exists_")) return null;
            // check # exists is related not only existentially quantified events
            if (didSth) {
                Set<Integer> boundedEvents = substTerms.stream()
                        .flatMap(x -> x.events().stream())
                        .collect(Collectors.toSet());
                boolean containsForallEvent = false;
                for (int i = 0; i < QUANTIFIERS.length - numExists; ++i) {
                    if (boundedEvents.contains(i)) {
                        containsForallEvent = true;
                        break;
                    }
                }
                if (!containsForallEvent) return null;
            }
        }
        return runSubst(line);
    }

    private String runSubst(String line) {
        for (var subst : substs.entrySet()) {
            line = line.replace(subst.getKey(), subst.getValue());
        }
        if (line.contains(" in ")) {
            line = line.replace(" in ", " == ");
            line = line.replace("[]", "");
        }
        return line;
    }

    private boolean isNumber(String x) {
        if (x == null || x.isEmpty()) return false;
        try {
            Double.parseDouble(x);
        } catch (NumberFormatException e) {
            return false;
        }
        return true;
    }

    private boolean containsTerm(String line, int forallTermCount, int existsTermCount) {
        for (int i = 0; i < forallTermCount + existsTermCount; ++i) {
            if (line.contains("f" + i)) return true;
        }
        return false;
    }

    private Map.Entry<List<Main.RawTerm>, List<Main.RawTerm>> 
        getTermSubsts(String line, List<Main.RawTerm> forallTerms, List<Main.RawTerm> existsTerms) {
        List<Main.RawTerm> forallSubsts = new ArrayList<>();
        List<Main.RawTerm> existsSubsts = new ArrayList<>();
        for (int i = 0; i < forallTerms.size(); ++i) {
            if (line.contains("f" + i)) forallSubsts.add(forallTerms.get(i));
        }
        for (int i = 0; i < existsTerms.size(); ++i) {
            int j = i + forallTerms.size();
            if (line.contains("f" + j)) existsSubsts.add(existsTerms.get(i));
        }
        return new AbstractMap.SimpleEntry<>(forallSubsts, existsSubsts);
    }

    private boolean checkValidity(String line, List<Main.RawPredicate> guards,
                                    List<Main.RawPredicate> filters,
                                    List<Main.RawTerm> forallTerms,
                                    List<Main.RawTerm> existsTerms) {
        if (pruningLevel == 0) {
            return true;
        }
        if (pruningLevel >= 1) {
            // prune nullptr comparisons
            // and type name comparisons
            for (var stub : FILTERED_INVS) {
                if (line.contains(stub)) return false;
            }
        }
        if (pruningLevel >= 2) {
            // prune if the set of terms of the mined property
            // equals to some selected atomic predicates
            Set<Integer> minedPropertyBoundedTerms = new HashSet<>();
            for (int i = 0; i < forallTerms.size(); ++i) {
                if (line.contains("f" + i)) {
                    minedPropertyBoundedTerms.add(forallTerms.get(i).order());
                }
            }
            for (int i = 0; i < existsTerms.size(); ++i) {
                if (line.contains("f" + (i + forallTerms.size()))) {
                    minedPropertyBoundedTerms.add(existsTerms.get(i).order());
                }
            }
            for (Main.RawPredicate p: guards) {
                if (p.terms().equals(minedPropertyBoundedTerms)) return false;
            }
            for (Main.RawPredicate p: filters) {
                if (p.terms().equals(minedPropertyBoundedTerms)) return false;
            }
        }
        if (pruningLevel >= 3) {
            // exclude comparisons with constants
            for (String op: COMP_OPS) {
                if (line.contains(op)) {
                    String[] args = line.split(op);
                    if (args.length < 2) continue;
                    String lhs = args[0].trim();
                    String rhs = args[1].trim();
                    boolean rhsIsConst = isNumber(rhs) || (rhs.startsWith("\"") && rhs.endsWith("\""));
                    if (!containsTerm(lhs, forallTerms.size(), existsTerms.size())) {
                        if (!rhsIsConst && !rhs.startsWith("size")) {
                            // if a comparison does not involve a term on lhs,
                            // it should not involve a term on rhs either (avoid `_num_e_exists_ > [some term]`)
                            // however, we do want to see size([some term]) on rhs.
                            // System.out.println("Filtered: " + line + " rhs: " + rhs);
                            return false;
                        }
                        break;
                    }
                    if (rhsIsConst) {
                        return false;
                    }
                    break;
                }
            }
        }
        return true;
    }
}
