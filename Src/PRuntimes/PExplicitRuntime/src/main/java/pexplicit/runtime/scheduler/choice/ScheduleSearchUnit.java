package pexplicit.runtime.scheduler.choice;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.machine.PMachineId;
import pexplicit.runtime.scheduler.explicit.StepState;

import java.util.ArrayList;
import java.util.List;

@Getter
@Setter
public class ScheduleSearchUnit extends SearchUnit<ScheduleChoice> {
    private StepState choiceState = null;

    /**
     * Constructor
     */
    public ScheduleSearchUnit(int stepNum, int choiceNum, ScheduleChoice c, List<ScheduleChoice> u, StepState s) {
        super(c, u, stepNum, choiceNum);
        this.choiceState = s;
    }

    /**
     * Clean unexplored choices
     */
    public void clearUnexplored() {
        unexplored.clear();
    }

    public SearchUnit copyCurrent() {
        return new ScheduleSearchUnit(this.stepNumber, this.choiceNumber, this.current, new ArrayList<>(), this.choiceState);
    }

    public SearchUnit transferChoice() {
        ScheduleSearchUnit newChoice = new ScheduleSearchUnit(this.stepNumber, this.choiceNumber, this.current, this.unexplored, this.choiceState);
        this.unexplored = new ArrayList<>();
        return newChoice;
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        if (current != null) {
            sb.append(String.format("curr@%s", current));
        }
        if (unexplored != null && !unexplored.isEmpty()) {
            sb.append(String.format(" rem@%s", unexplored));
        }
        return sb.toString();
    }
}
