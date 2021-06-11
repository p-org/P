package psymbolic.valuesummary.bdd;

import org.sosy_lab.pjbdd.api.Builders;
import org.sosy_lab.pjbdd.api.Creator;
import org.sosy_lab.pjbdd.api.CreatorBuilder;
import org.sosy_lab.pjbdd.api.DD;
import org.sosy_lab.pjbdd.util.parser.BDDStringImporter;
import org.sosy_lab.pjbdd.util.parser.DotExporter;
import org.sosy_lab.pjbdd.util.parser.Exporter;
import org.sosy_lab.pjbdd.util.parser.Importer;

/**
 * Represents the BDD implementation using PJBDD
 */
public class PJBDDImpl {
    final private Creator c;
    final private Exporter<DD> e;
    final private Importer<DD> i;

    // configurable parameters for PJBDD
    // TODO: Explore different options for these parameters
    int numThreads = Runtime.getRuntime().availableProcessors();
    int cacheSize = 10000000;

    public PJBDDImpl(boolean cbdd) {
        CreatorBuilder creatorBuilder = Builders.cbddBuilder();
        if (!cbdd) {
            creatorBuilder = Builders.bddBuilder();
        }
        c = creatorBuilder
                .setVarCount(0)
                .setParallelizationType(Builders.ParallelizationType.NONE)
                .setIncreaseFactor(5)
                .setTableSize(1000000)
                .setCacheSize(cacheSize)
                .build();
        e = new DotExporter();
        i = new BDDStringImporter(c);
    }

    public DD constFalse() {
        return c.makeFalse();
    }

    public DD constTrue() {
        return c.makeTrue();
    }

    public boolean isFalse(DD bdd) {
        return bdd.isFalse();
    }

    public boolean isTrue(DD bdd) {
        return bdd.isTrue();
    }

    public DD and(DD left, DD right) {
        return c.makeAnd(left, right);
    }

    public DD or(DD left, DD right) {
        return c.makeOr(left, right);
    }

    public DD not(DD bdd) {
        return c.makeNot(bdd);
    }

    public DD implies(DD left, DD right) {
        return c.makeImply(left, right);
    }

    public DD ifThenElse(DD cond, DD thenClause, DD elseClause) {
        return c.makeIte(cond, thenClause, elseClause);
    }

    public DD newVar() {
        return c.makeVariable();
    }

    public String toString(DD bdd) {
        if (bdd == null) return "null";
        if (bdd.isFalse()) return "false";
        if (bdd.isTrue()) return "true";
        return e.bddToString(bdd);
    }

    public DD fromString(String s) {
        if (s.equals("false")) {
            return c.makeFalse();
        }
        if (s.equals("true")) {
            return c.makeTrue();
        }
        return i.bddFromString(s);
    }
}
