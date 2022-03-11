package psymbolic.valuesummary.solvers.sat;

import com.berkeley.abc.Abc;
import com.sri.yices.Yices;
import psymbolic.runtime.statistics.SolverStats;

import java.io.ByteArrayOutputStream;
import java.io.PrintStream;
import java.util.List;

public class ABCImpl implements SatLib<Long> {
    public Long constFalse() {
        return Aig.getFalse();
    }

    public Long constTrue() {
        return Aig.getTrue();
    }

    public boolean isSat(Long formula) {
        SolverStats.isSatOperations++;
        switch(Aig.isSat(formula, -1)) {
            case Sat:
                SolverStats.isSatResult++;
                return true;
            case Unsat:
                return false;
            default:
                throw new RuntimeException("ABC returned query result unknown for " + toString(formula));
        }
    }

    public Long and(List<Long> children) {
        throw new RuntimeException("Unsupported");
    }

    public Long or(List<Long> children) {
        throw new RuntimeException("Unsupported");
    }

    public Long not(Long formula) {
        throw new RuntimeException("Unsupported");
    }

    public Long newVar(String name) {
        throw new RuntimeException("Unsupported");
    }

    public String toString(Long formula) {
        return Aig.toString(formula);
    }

    public Long fromString(String s) {
        throw new RuntimeException("Unsupported");
    }

    public int getNodeCount() {
        return Aig.idSet.size();
    }

    public String getStats() {
        return Aig.getStats();
    }

    public void cleanup() {
        // TODO
    }

    public boolean areEqual(Long left, Long right) {
        return Aig.areEqual(left, right);
    }

}
