package pexplicit.values;

import lombok.Getter;
import org.apache.commons.lang3.ArrayUtils;

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
    public PString(String base, PValue<?>... args) {
        this.base = base;
        if (args == null || args.length == 0) {
            this.args = null;
            this.value = base;
        } else {
            this.args = new PValue<?>[args.length];
            for (int i = 0; i < args.length; i++) {
                this.args[i] = PValue.clone(args[i]);
            }
            this.value = MessageFormat.format(base, args);
        }
        initialize();
    }

    /**
     * Constructor
     *
     * @param val PString value to construct from.
     */
    public PString(PString val) {
        this(val.base, val.args);
    }

    /**
     * Concatenation operation
     *
     * @param val PString value to concatenate
     * @return PString object after operation
     */
    public PString add(PString val) {
        String newBase = this.base.concat(val.base);
        PValue<?>[] newArgs = ArrayUtils.addAll(this.args, val.args);
        return new PString(newBase, newArgs);
    }

    /**
     * Less than operation
     *
     * @param val PString value to compare to
     * @return PBool object after operation
     */
    public PBool lt(PString val) {
        return new PBool(this.value.compareTo(val.value) < 0);
    }

    /**
     * Less than or equal to operation
     *
     * @param val PString value to compare to
     * @return PBool object after operation
     */
    public PBool le(PString val) {
        return new PBool(this.value.compareTo(val.value) <= 0);
    }

    /**
     * Greater than operation
     *
     * @param val PString value to compare to
     * @return PBool object after operation
     */
    public PBool gt(PString val) {
        return new PBool(this.value.compareTo(val.value) > 0);
    }

    /**
     * Greater than or equal to operation
     *
     * @param val PString value to compare to
     * @return PBool object after operation
     */
    public PBool ge(PString val) {
        return new PBool(this.value.compareTo(val.value) >= 0);
    }

    @Override
    public PString clone() {
        return new PString(base, args);
    }

    @Override
    protected String _asString() {
        return value;
    }

    @Override
    public PString getDefault() {
        return new PString("");
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof PString)) {
            return false;
        }
        return this.value.equals(((PString) obj).value);
    }
}
