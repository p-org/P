package psymbolic.runtime.concretevalues;

public class PString extends PValue<PString> {
    // stores the int value
    private final String value;

    public String getValue() { return value; }

    public PString(String val)
    {
        value = val;
    }

    public PString(PString val)
    {
        value = val.value;
    }

    @Override
    public PString clone() {
        return new PString(value);
    }

    @Override
    public int hashCode() {
        return value.hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof PString)) {
            return false;
        }
        return this.value.equals(((PString)obj).value);
    }

    @Override
    public String toString() {
        return value;
    }
}
