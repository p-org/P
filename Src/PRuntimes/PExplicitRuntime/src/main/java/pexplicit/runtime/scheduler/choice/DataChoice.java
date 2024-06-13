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
    public DataChoice(int stepNum, int choiceNum, PValue<?> c) {
        super(c, stepNum, choiceNum);
    }

    public Choice copyCurrent() {
        return new DataChoice(this.stepNumber, this.choiceNumber, this.current);
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
