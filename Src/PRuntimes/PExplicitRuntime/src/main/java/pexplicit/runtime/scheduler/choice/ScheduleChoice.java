package pexplicit.runtime.scheduler.choice;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.machine.PMachineId;
import pexplicit.runtime.scheduler.explicit.StepState;

@Getter
@Setter
public class ScheduleChoice extends Choice<PMachineId> {
    /**
     * Step number
     */
    private int stepNumber = 0;
    /**
     * Choice number
     */
    private int choiceNumber = 0;

    /**
     * Protocol state at the schedule step
     */
    private StepState choiceState = null;

    /**
     * Constructor
     */
    public ScheduleChoice(int stepNum, int choiceNum, PMachineId c, StepState s) {
        super(c);
        this.stepNumber = stepNum;
        this.choiceNumber = choiceNum;
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
