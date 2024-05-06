package pexplicit.runtime.scheduler.choice;

import lombok.Getter;
import lombok.Setter;
import pexplicit.values.PValue;

import java.util.ArrayList;
import java.util.List;

@Getter
@Setter
public class DataChoice extends Choice<PValue<?>> {
    /**
     * Constructor
     */
    public DataChoice(int stepNum, int choiceNum, PValue<?> c, List<PValue<?>> u) {
        super(c, u, stepNum, choiceNum);
    }

    /**
     * Clean unexplored choices
     */
    public void clearUnexplored() {
        unexplored.clear();
    }

    public Choice copyCurrent() {
        return new DataChoice(this.stepNumber, this.choiceNumber, this.current, new ArrayList<>());
    }

    public Choice transferChoice() {
        DataChoice newChoice = new DataChoice(this.stepNumber, this.choiceNumber, this.current, this.unexplored);
        this.unexplored = new ArrayList<>();
        return newChoice;
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        if (current != null) {
            sb.append(String.format("curr:%s", current));
        }
        if (unexplored != null && !unexplored.isEmpty()) {
            sb.append(String.format(" rem:%s", unexplored));
        }
        return sb.toString();
    }
}
