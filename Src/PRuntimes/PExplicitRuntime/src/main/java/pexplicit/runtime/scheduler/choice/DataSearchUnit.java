package pexplicit.runtime.scheduler.choice;

import lombok.Getter;
import lombok.Setter;
import pexplicit.values.PValue;

import java.util.ArrayList;
import java.util.List;

@Getter
@Setter
public class DataSearchUnit extends SearchUnit<DataChoice> {
    /**
     * Constructor
     */
    public DataSearchUnit(int stepNum, int choiceNum, DataChoice c, List<DataChoice> u) {
        super(c, u, stepNum, choiceNum);
    }

    /**
     * Clean unexplored choices
     */
    public void clearUnexplored() {
        unexplored.clear();
    }

    public SearchUnit copyCurrent() {
        return new DataSearchUnit(this.stepNumber, this.choiceNumber, this.current, new ArrayList<>());
    }

    public SearchUnit transferChoice() {
        DataSearchUnit newChoice = new DataSearchUnit(this.stepNumber, this.choiceNumber, this.current, this.unexplored);
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
