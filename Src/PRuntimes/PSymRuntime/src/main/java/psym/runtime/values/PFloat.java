package psym.runtime.values;


public class PFloat extends PValue<PFloat> {
    // stores the int value
    private final double value;

    public PFloat(double val) {
        value = val;
    }

    public PFloat(Object val) {
        if (val instanceof PFloat)
            value = ((PFloat) val).value;
        else
            value = (double) val;
    }

    public PFloat(PFloat val) {
        value = val.value;
    }

    public double getValue() {
        return value;
    }

    @Override
    public PFloat clone() {
        return new PFloat(value);
    }

    @Override
    public int hashCode() {
        return Double.valueOf(value).hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof PFloat)) {
            return false;
        }
        return this.value == ((PFloat) obj).value;
    }

    @Override
    public String toString() {
        return Double.toString(value);
    }
}
