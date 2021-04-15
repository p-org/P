package symbolicp.bdd;

import symbolicp.runtime.ScheduleLogger;

import java.io.FileNotFoundException;
import java.io.PrintWriter;
import java.time.Duration;
import java.time.Instant;
import java.util.List;

/**
 * This class determines the global BDD implementation used by the symbolic engine.
 *
 * It is a thin wrapper over a BddLib, which can be swapped out at will by reassigning the `globalBddLib` variable
 * and adjusting the implementation of reset();
 */
public class InstBdd extends Bdd {
    static int trueQueries = 0;
    static int falseQueries = 0;

    private static Bdd bdd;

    public InstBdd(Object wrappedBdd) { super(wrappedBdd); }

    public boolean isConstFalse() {
        falseQueries++; return super.isConstFalse();
    }

    public boolean isConstTrue() {
        trueQueries++; return super.isConstTrue();
    }

    private void updateBookkeeping(Duration length) {

    }

    public Bdd and(Bdd other) {
        Instant start = Instant.now();
        Bdd res = super.and(other);
        Instant end = Instant.now();
        Duration length = Duration.between(start, end);
        updateBookkeeping(length);
        return res;
    }

    public Bdd or(Bdd other) {
        Instant start = Instant.now();
        Bdd res = super.or(other);
        Instant end = Instant.now();
        Duration length = Duration.between(start, end);
        updateBookkeeping(length);
        return res;
    }

    public Bdd implies(Bdd other) {
        Instant start = Instant.now();
        Bdd res = super.implies(other);
        Instant end = Instant.now();
        Duration length = Duration.between(start, end);
        updateBookkeeping(length);
        return res;
    }

    public Bdd not() {
        Instant start = Instant.now();
        Bdd res = super.not();
        Instant end = Instant.now();
        Duration length = Duration.between(start, end);
        updateBookkeeping(length);
        return res;
    }

    public static Bdd orMany(List<Bdd> wrappedBdd) {
        Instant start = Instant.now();
        Bdd res = Bdd.orMany(wrappedBdd);
        return wrappedBdd.stream().reduce(Bdd.constFalse(), Bdd::or);
    }

    public Bdd ifThenElse(Bdd thenCase, Bdd elseCase) {
        Instant start = Instant.now();
        Bdd res = super.ifThenElse(thenCase, elseCase);
        Instant end = Instant.now();
        Duration length = Duration.between(start, end);
        updateBookkeeping(length);
        return res;
    }
}

