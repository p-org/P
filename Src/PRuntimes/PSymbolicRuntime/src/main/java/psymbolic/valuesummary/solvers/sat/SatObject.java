package psymbolic.valuesummary.solvers.sat;

public class SatObject {
    public Object formula;
    public SatStatus status;

    public SatObject(Object formula, SatStatus stat) {
        this.formula = formula;
        this.status = stat;
    }
}
