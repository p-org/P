package symbolicp.bdd;

import java.util.List;

/**
 * This class determines the global BDD implementation used by the symbolic engine.
 *
 * It is a thin wrapper over a BddLib, which can be swapped out at will by reassigning the `globalBddLib` variable
 * and adjusting the implementation of reset();
 */
public class Bdd {

    private static BddLib newBddImpl() { return new PjbddImpl(false); }

    private static BddLib globalBddLib = newBddImpl();

    /** Reset the BDD library by making a new one */
    public static void reset() {
        globalBddLib = newBddImpl();
    }

    private final Object wrappedBdd;

    public Bdd(Object wrappedBdd) {
        this.wrappedBdd = wrappedBdd;
    }

    /** Return new constant false BDD
     *
     * @return BDD for the constant false
     */
    public static Bdd constFalse() {
        return new Bdd(globalBddLib.constFalse());
    }

    /** Return new constant true BDD
     *
     * @return BDD for the constant true
     */
    public static Bdd constTrue() {
        return new Bdd(globalBddLib.constTrue());
    }

    /** Return whether the BDD is constant false
     *
     * @return True iff BDD is the constant false
     */
    public boolean isConstFalse() {
        return globalBddLib.isConstFalse(wrappedBdd);
    }

    /** Return whether the BDD is constant true
     *
     * @return True iff BDD is the constant true
     */
    public boolean isConstTrue() {
        return globalBddLib.isConstTrue(wrappedBdd);
    }

    /** Conjoin BDD with another BDD
     *
     * @param other The other BDD conjunct
     * @return The resulting BDD from the conjunction
     */
    public Bdd and(Bdd other) {
        Bdd res = new Bdd(globalBddLib.and(wrappedBdd, other.wrappedBdd));
        return res;
    }

    /** Disjoin BDD with another BDD
     *
     * @param other The other BDD disjunct
     * @return The resulting BDD from the disjunction
     */
    public Bdd or(Bdd other) {
        Bdd res = new Bdd(globalBddLib.or(wrappedBdd, other.wrappedBdd));
        return res;
    }

    /** Make BDD this BDD imply another BDD
     *
     * @param other The consequent of the implication
     * @return The resulting BDD from the implication
     */
    public Bdd implies(Bdd other) { return new Bdd(globalBddLib.implies(wrappedBdd, other.wrappedBdd)); }

    /** Negate this BDD
     *
     * @return The result of the negation
     */
    public Bdd not() {
        Bdd res = new Bdd(globalBddLib.not(wrappedBdd));
        return res;
    }

    /** Disjoing BDD with several other BDDs
     *
     * @param wrappedBdds List of other BDDs
     * @return The resulting BDD from the disjunction
     */
    public static Bdd orMany(List<Bdd> wrappedBdds) {
        return wrappedBdds.stream().reduce(Bdd.constFalse(), Bdd::or);
    }

    /** Create an if-then-else using this BDD as the condition
     *
     * @param thenCase The "then" case BDD
     * @param elseCase The "else" case BDD
     * @return The result of constructing the if-then-else
     */
    public Bdd ifThenElse(Bdd thenCase, Bdd elseCase) {
        return new Bdd(globalBddLib.ifThenElse(wrappedBdd, thenCase.wrappedBdd, elseCase.wrappedBdd));
    }

    /** Create a new BDD variable
     *
     * @return The new BDD variable as a BDD
     */
    public static Bdd newVar() {
        return new Bdd(globalBddLib.newVar());
    }

    @Override
    public String toString() {
        return globalBddLib.toString(wrappedBdd);
    }
}
