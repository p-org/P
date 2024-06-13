package pexplicit.runtime.scheduler.choice;

import lombok.Getter;
import lombok.Setter;
import pexplicit.values.PValue;

@Getter
@Setter
public class DataChoice extends Choice<PValue<?>> {
    /**
     * Constructor
     */
    public DataChoice(PValue<?> c) {
        super(c);
    }

    public Choice copyCurrent() {
        return new DataChoice(this.current);
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        if (current != null) {
            sb.append(String.format("curr:%s", current));
        }
        return sb.toString();
    }
}
