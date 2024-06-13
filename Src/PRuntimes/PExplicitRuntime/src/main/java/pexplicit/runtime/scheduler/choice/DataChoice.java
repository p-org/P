package pexplicit.runtime.scheduler.choice;

import pexplicit.values.PValue;

public class DataChoice extends Choice<PValue<?>> {
    public DataChoice(PValue<?> value) {
        super(value);
    }

    @Override
    public String toString() {
        return value.toString();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof DataChoice)) {
            return false;
        }
        return this.value == ((DataChoice) obj).value;
    }
}
