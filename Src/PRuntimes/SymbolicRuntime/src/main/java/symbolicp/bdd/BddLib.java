package symbolicp.bdd;

public interface BddLib<Bdd> {
    Bdd constFalse();

    Bdd constTrue();

    boolean isConstFalse(Bdd bdd);

    boolean isConstTrue(Bdd bdd);

    Bdd and(Bdd left, Bdd right);

    Bdd or(Bdd left, Bdd right);

    Bdd not(Bdd bdd);

    Bdd implies(Bdd left, Bdd right);

    Bdd ifThenElse(Bdd cond, Bdd thenClause, Bdd elseClause);

    Bdd newVar();

    String toString(Bdd bdd);

    Bdd fromString(String s);
}

