package pexplicit.values;

import lombok.Getter;

/**
 * Represents the PValue for P enum
 */
@Getter
public class PEnum extends PValue<PEnum> {
    private final String name;
    private final int value;

    /**
     * Constructor
     *
     * @param name Name of the enum
     * @param val  integer value
     */
    public PEnum(String name, int val) {
        this.name = name;
        value = val;
    }

    /**
     * Copy constructor
     *
     * @param val PEnum value to copy from
     */
    public PEnum(PEnum val) {
        name = val.name;
        value = val.value;
    }

    @Override
    public PEnum clone() {
        return new PEnum(this);
    }

    @Override
    public int hashCode() {
        return name.hashCode() ^ Long.hashCode(value);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof PEnum)) {
            return false;
        }
        return this.value == ((PEnum) obj).value && this.name.equals(((PEnum) obj).name);
    }

    @Override
    public String toString() {
        return name;
    }
}
