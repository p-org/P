package symbolicp.bdd;

import org.sosy_lab.pjbdd.Builders;
import org.sosy_lab.pjbdd.creator.bdd.Creator;
import org.sosy_lab.pjbdd.node.BDD;
import org.sosy_lab.pjbdd.util.parser.*;

public class PjbddImpl implements BddLib<BDD> {
    final private Creator c;
    final private Exporter e;
    final private Importer i;

    public PjbddImpl() {
        c = Builders.newBDDBuilder().setVarCount(0).build();
        e = new DotExporter();
        i = new BDDStringImporter(c);
    }

    @Override
    public BDD constFalse() {
        return c.makeFalse();
    }

    @Override
    public BDD constTrue() {
        return c.makeTrue();
    }

    @Override
    public boolean isConstFalse(BDD bdd) {
        return bdd.isFalse();
    }

    @Override
    public boolean isConstTrue(BDD bdd) {
        return bdd.isTrue();
    }

    @Override
    public BDD and(BDD left, BDD right) {
        return c.makeAnd(left, right);
    }

    @Override
    public BDD or(BDD left, BDD right) {
        return c.makeOr(left, right);
    }

    @Override
    public BDD not(BDD bdd) {
        return c.makeNot(bdd);
    }

    @Override
    public BDD implies(BDD left, BDD right) {
        return c.makeImply(left, right);
    }

    @Override
    public BDD ifThenElse(BDD cond, BDD thenClause, BDD elseClause) {
        return c.makeIte(cond, thenClause, elseClause);
    }

    @Override
    public BDD newVar() {
        return c.makeVariable();
    }

    @Override
    public String toString(BDD bdd) {
        if (bdd == null) return "null";
        if (bdd.isFalse()) return "false";
        if (bdd.isTrue()) return "true";
        return e.bddToString(bdd);
    }

    @Override
    public BDD fromString(String s) {
        if (s.equals("false")) {
            return c.makeFalse();
        }
        if (s.equals("true")) {
            return c.makeTrue();
        }
        return i.bddFromString(s);
    }
}
