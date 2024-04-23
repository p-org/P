package pexplicit.values;

import lombok.Getter;

/**
 * Represents the PValue for P enum
 */
@Getter
public class PEnum extends PValue<PEnum> {
    private final String type;
    private final String name;
    private final int value;

    /**
     * Constructor
     *
     * @param name Name of the enum
     * @param val  integer value
     */
    public PEnum(String type, String name, int val) {
        this.type = type;
        this.name = name;
        this.value = val;
        setRep();
    }

    /**
     * Copy constructor
     *
     * @param val PEnum value to copy from
     */
    public PEnum(PEnum val) {
        type = val.type;
        name = val.name;
        value = val.value;
        setRep();
    }

    /**
     * Convert to a PInt
     *
     * @return PInt object
     */
    public PInt toInt() {
        return new PInt(value);
    }

    @Override
    public PEnum clone() {
        return new PEnum(this);
    }

    @Override
    protected void setHashCode() {
        hashCode = Long.hashCode(value);
    }

    @Override
    protected void setStringRep() {
        stringRep = name;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof PEnum)) {
            return false;
        }
        return this.value == ((PEnum) obj).value;
    }
}
