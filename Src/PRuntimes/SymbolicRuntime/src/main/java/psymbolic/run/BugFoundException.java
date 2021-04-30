package psymbolic.run;

import psymbolic.valuesummary.bdd.Bdd;

public class BugFoundException extends RuntimeException {
    public final Bdd pathConstraint;

    public BugFoundException(String message, Bdd pathConstraint) {
        super(message);
        this.pathConstraint = pathConstraint;
    }
}
