package psym.valuesummary.solvers.bdd;

import org.sosy_lab.pjbdd.api.Builders;
import org.sosy_lab.pjbdd.api.Creator;
import org.sosy_lab.pjbdd.api.CreatorBuilder;
import org.sosy_lab.pjbdd.api.DD;
import org.sosy_lab.pjbdd.util.parser.BDDStringImporter;
import org.sosy_lab.pjbdd.util.parser.DotExporter;
import org.sosy_lab.pjbdd.util.parser.Exporter;
import org.sosy_lab.pjbdd.util.parser.Importer;
import psym.runtime.statistics.SolverStats;
import psym.valuesummary.solvers.SolverLib;

/** Represents the BDD implementation using PJBDD */
public class PJBDDImpl implements SolverLib<DD> {
  private final Creator c;
  private final Exporter<DD> e;
  private final Importer<DD> i;

  // configurable parameters for PJBDD
  // TODO: Explore different options for these parameters
  private final int numThreads = Runtime.getRuntime().availableProcessors();
  private final int cacheSize = 100000;

  public PJBDDImpl(boolean cbdd) {
    CreatorBuilder creatorBuilder = Builders.cbddBuilder();
    if (!cbdd) {
      creatorBuilder = Builders.bddBuilder();
    }
    c =
        creatorBuilder
            .setVarCount(0)
            .disableThreadSafety()
            // .setParallelism(numThreads)
            .setParallelizationType(Builders.ParallelizationType.NONE)
            .setTableSize(10000)
            .setCacheSize(cacheSize)
            .build();
    e = new DotExporter();
    i = new BDDStringImporter(c);
    //        System.out.println("Using BDDs");
  }

  public DD constFalse() {
    return c.makeFalse();
  }

  public DD constTrue() {
    return c.makeTrue();
  }

  public boolean isSat(DD bdd) {
    SolverStats.isSatOperations++;
    boolean result = !bdd.isFalse();
    if (result) {
      SolverStats.isSatResult++;
    }
    return result;
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

  public DD newVar(String name) {
    return c.makeVariable();
  }

  public DD simplify(DD bdd) {
    return bdd;
  }

  public String toString(DD bdd) {
    if (bdd == null) return "null";
    if (bdd.isFalse()) return "false";
    if (bdd.isTrue()) return "true";
    e.export(bdd, "./dot/");
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

  public int getVarCount() {
    return c.getVariableCount();
  }

  public int getNodeCount() {
    return c.getCreatorStats().getNodeCount();
  }

  public int getExprCount() {
    return getNodeCount();
  }

  public String getStats() {
    return c.getCreatorStats().prettyPrint();
  }

  public void cleanup() {
    c.cleanUnusedNodes();
  }

  public boolean areEqual(DD left, DD right) {
    return left.equals(right);
  }

  public int hashCode(DD formula) {
    return formula.hashCode();
  }
}
