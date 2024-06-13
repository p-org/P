package pexplicit.runtime.scheduler.choice;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.machine.PMachineId;
import pexplicit.runtime.scheduler.explicit.StepState;

import java.util.ArrayList;
import java.util.List;

@Getter
@Setter
public class ScheduleChoice extends Choice<PMachineId> {
    private StepState choiceState = null;

    /**
     * Constructor
     */
    public ScheduleChoice(int stepNum, int choiceNum, PMachineId c, StepState s) {
        super(c, stepNum, choiceNum);
        this.choiceState = s;
    }

    public Choice copyCurrent() {
        return new ScheduleChoice(this.stepNumber, this.choiceNumber, this.current, this.choiceState);
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        if (current != null) {
            sb.append(String.format("curr@%s", current));
        }
        return sb.toString();
    }
}
