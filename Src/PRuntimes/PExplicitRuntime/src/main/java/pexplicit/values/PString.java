package pexplicit.values;

import lombok.Getter;

import java.text.MessageFormat;

/**
 * Represents the PValue for P string
 */
@Getter
public class PString extends PValue<PString> {
    private final String base;
    private final PValue<?>[] args;
    private final String value;

    /**
     * Constructor
     *
     * @param base Base string.
     * @param args Arguments, if any.
     */
    public PString(String base, PValue<?> ... args) {
        this.base = base;
        if (args.length == 0) {
            this.args = null;
            this.value = base;
        } else {
            this.args = new PValue<?>[args.length];
            for (int i = 0; i < args.length; i++) {
                this.args[i] = PValue.clone(args[i]);
            }
            this.value = MessageFormat.format(base, args);
        }
    }

    /**
     * Constructor
     *
     * @param val PString value to construct from.
     */
    public PString(PString val) {
        this(val.base, val.args);
    }

    @Override
    public PString clone() {
        return new PString(base, args);
    }

    @Override
    public int hashCode() {
        return value.hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof PString)) {
            return false;
        }
        return this.value.equals(((PString) obj).value);
    }

    @Override
    public String toString() {
        return value;
    }
}
