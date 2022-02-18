package psymbolic.valuesummary.solvers.sat;

import org.sosy_lab.common.ShutdownManager;
import org.sosy_lab.common.configuration.Configuration;
import org.sosy_lab.common.configuration.InvalidConfigurationException;
import org.sosy_lab.common.log.BasicLogManager;
import org.sosy_lab.common.log.LogManager;
import org.sosy_lab.java_smt.SolverContextFactory;
import org.sosy_lab.java_smt.SolverContextFactory.Solvers;
import org.sosy_lab.java_smt.api.*;
import psymbolic.valuesummary.solvers.SolverLib;
import psymbolic.valuesummary.solvers.SolverType;

/**
 * Represents the Sat implementation using JavaSMT
 */
public class JavaSmtImpl implements SolverLib<BooleanFormula> {
    final private SolverContext context;
    final private FormulaManager formulaManager;
    final private BooleanFormulaManager booleanFormulaManager;
    
    private long idx = 0;

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

    public boolean isFalse(BooleanFormula formula) {
        try (ProverEnvironment prover = context.newProverEnvironment()) {
            prover.addConstraint(formula);
            boolean isUnsat = prover.isUnsat();
            return isUnsat;
        } catch(InterruptedException | SolverException e) {
            e.printStackTrace();
            throw new RuntimeException("Issue querying solver");
        }
    }
    
    public boolean isTrue(BooleanFormula formula) {
        try (ProverEnvironment prover = context.newProverEnvironment()) {
            prover.addConstraint(formula);
            boolean isSat = !prover.isUnsat();
            return isSat;
        } catch(InterruptedException | SolverException e) {
            e.printStackTrace();
            throw new RuntimeException("Issue querying solver");
        }
    }

    public BooleanFormula and(BooleanFormula left, BooleanFormula right) {
        return booleanFormulaManager.and(left, right);
    }

    public BooleanFormula or(BooleanFormula left, BooleanFormula right) {
        return booleanFormulaManager.or(left, right);
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

    public BooleanFormula newVar() {
        return booleanFormulaManager.makeVariable("x" + idx++);
    }

    public String toString(BooleanFormula booleanFormula) {
        return booleanFormula.toString();
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

    public int getVarCount() {
    	// TODO
        return 0;
    }

    public int getNodeCount() {
    	// TODO
        return 0;
    }

    public String getStats() {
    	// TODO
        return "";
    }

    public void cleanup() {
    	// TODO
    }
}
