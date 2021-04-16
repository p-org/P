package symbolicp.bdd;

import org.sosy_lab.pjbdd.api.Builders;
import org.sosy_lab.pjbdd.api.Creator;
import org.sosy_lab.pjbdd.api.DD;
import org.sosy_lab.pjbdd.util.parser.*;

public class PjbddImpl implements BddLib<DD> {
    final private Creator c;
    final private Exporter<DD> e;
    final private Importer<DD> i;

    public PjbddImpl() {
        c = Builders.bddBuilder().setVarCount(0).build();
        e = new DotExporter();
        i = new BDDStringImporter(c);
    }

    @Override
    public DD constFalse() {
        return c.makeFalse();
    }

    @Override
    public DD constTrue() {
        return c.makeTrue();
    }

    @Override
    public boolean isConstFalse(DD bdd) {
        return bdd.isFalse();
    }

    @Override
    public boolean isConstTrue(DD bdd) {
        return bdd.isTrue();
    }

    @Override
    public DD and(DD left, DD right) {
        return c.makeAnd(left, right);
    }

    @Override
    public DD or(DD left, DD right) {
        return c.makeOr(left, right);
    }

    @Override
    public DD not(DD bdd) {
        return c.makeNot(bdd);
    }

    @Override
    public DD implies(DD left, DD right) {
        return c.makeImply(left, right);
    }

    @Override
    public DD ifThenElse(DD cond, DD thenClause, DD elseClause) {
        return c.makeIte(cond, thenClause, elseClause);
    }

    @Override
    public DD newVar() {
        return c.makeVariable();
    }

    @Override
    public String toString(DD bdd) {
        if (bdd == null) return "null";
        if (bdd.isFalse()) return "false";
        if (bdd.isTrue()) return "true";
        return e.bddToString(bdd);
    }

    @Override
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
