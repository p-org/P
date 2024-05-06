package pexplicit.runtime.scheduler.choice;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.scheduler.explicit.StepState;

import java.util.ArrayList;
import java.util.List;

@Getter
@Setter
public class ScheduleChoice extends Choice<PMachine> {
    private StepState choiceState = null;

    /**
     * Constructor
     */
    public ScheduleChoice(int stepNum, int choiceNum, PMachine c, List<PMachine> u, StepState s) {
        super(c, u, stepNum, choiceNum);
        this.choiceState = s;
    }

    /**
     * Clean unexplored choices
     */
    public void clearUnexplored() {
        unexplored.clear();
        choiceState = null;
    }

    public Choice copyCurrent() {
        return new ScheduleChoice(this.stepNumber, this.choiceNumber, this.current, new ArrayList<>(), null);
    }

    public Choice transferChoice() {
        ScheduleChoice newChoice = new ScheduleChoice(this.stepNumber, this.choiceNumber, this.current, this.unexplored, this.choiceState);
        this.unexplored = new ArrayList<>();
        this.choiceState = null;
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
