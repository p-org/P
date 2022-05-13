package psymbolic.valuesummary.solvers.sat;

import org.sosy_lab.common.ShutdownManager;
import org.sosy_lab.common.configuration.Configuration;
import org.sosy_lab.common.configuration.InvalidConfigurationException;
import org.sosy_lab.common.log.BasicLogManager;
import org.sosy_lab.common.log.LogManager;
import org.sosy_lab.java_smt.SolverContextFactory;
import org.sosy_lab.java_smt.SolverContextFactory.Solvers;
import org.sosy_lab.java_smt.api.*;
import psymbolic.runtime.statistics.SolverStats;
import psymbolic.valuesummary.solvers.SolverType;

import java.util.List;

/**
 * Represents the Sat implementation using JavaSMT
 */
public class JavaSmtImpl implements SatLib<BooleanFormula> {
    final private SolverContext context;
    final private FormulaManager formulaManager;
    final private BooleanFormulaManager booleanFormulaManager;

    public JavaSmtImpl(SolverType type) {
        try {
            Configuration config = Configuration.defaultConfiguration();
            LogManager logger = BasicLogManager.create(config);
            ShutdownManager shutdown = ShutdownManager.create();
            Solvers solver = getSolverType(type);

            context = SolverContextFactory.createSolverContext(
                    config, logger, shutdown.getNotifier(), solver);
            formulaManager = context.getFormulaManager();
            booleanFormulaManager = formulaManager.getBooleanFormulaManager();
        } catch (InvalidConfigurationException e){
            e.printStackTrace();
            throw new RuntimeException("Invalid configuration for SMT");
        }
    }
    
    private Solvers getSolverType(SolverType type) {
        switch (type) {
        case JAVASMT_BOOLECTOR:		return Solvers.BOOLECTOR;
        case JAVASMT_CVC4:			return Solvers.CVC4;
        case JAVASMT_MATHSAT5:		return Solvers.MATHSAT5;
        case JAVASMT_PRINCESS:		return Solvers.PRINCESS;
        case JAVASMT_SMTINTERPOL:	return Solvers.SMTINTERPOL;
        case JAVASMT_YICES2:		return Solvers.YICES2;
        case JAVASMT_Z3:			return Solvers.Z3;
        }
        return Solvers.Z3;
    }
    
    
    public BooleanFormula constFalse() {
        return booleanFormulaManager.makeFalse();
    }

    public BooleanFormula constTrue() {
        return booleanFormulaManager.makeTrue();
    }

    public boolean isSat(BooleanFormula formula) {
        SolverStats.isSatOperations++;
        try (ProverEnvironment prover = context.newProverEnvironment()) {
            prover.addConstraint(formula);
            boolean result = !prover.isUnsat();
            if (result)
                SolverStats.isSatResult++;
            return result;
        } catch(InterruptedException | SolverException e) {
            e.printStackTrace();
            throw new RuntimeException("Issue querying solver");
        }
    }

    public BooleanFormula and(List<BooleanFormula> children) {
        return booleanFormulaManager.and(children);
    }

    public BooleanFormula or(List<BooleanFormula> children) {
        return booleanFormulaManager.or(children);
    }

    public BooleanFormula not(BooleanFormula booleanFormula) {
        return booleanFormulaManager.not(booleanFormula);
    }

    public BooleanFormula implies(BooleanFormula left, BooleanFormula right) {
        return booleanFormulaManager.implication(left, right);
    }

    public BooleanFormula ifThenElse(BooleanFormula cond, BooleanFormula thenClause, BooleanFormula elseClause) {
        return booleanFormulaManager.or(booleanFormulaManager.and(cond, thenClause),
                booleanFormulaManager.and(booleanFormulaManager.not(cond), elseClause));
    }

    public BooleanFormula newVar(String name) {
        return booleanFormulaManager.makeVariable(name);
    }

    private BooleanFormula simplify(BooleanFormula booleanFormula) {
        try {
            return formulaManager.simplify(booleanFormula);
        } catch (InterruptedException e) {
            e.printStackTrace();
            throw new RuntimeException("Issue simplifying");
        }
    }

    public String toString(BooleanFormula booleanFormula) {
//        return booleanFormula.toString();
        return simplify(booleanFormula).toString();
    }

    public BooleanFormula fromString(String s) {
        if (s.equals("false")) {
            return booleanFormulaManager.makeFalse();
        }
        if (s.equals("true")) {
            return booleanFormulaManager.makeTrue();
        }
        throw new RuntimeException("Unsupported");
    }

    public int getNodeCount() {
        return -1;
    }

    public String getStats() {
    	// TODO
        return "";
    }

    public void cleanup() {
    	// TODO
    }

    public boolean areEqual(BooleanFormula left, BooleanFormula right) {
        return left.equals(right);
    }
}
