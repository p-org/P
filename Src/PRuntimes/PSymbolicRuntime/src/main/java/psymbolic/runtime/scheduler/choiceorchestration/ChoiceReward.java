package psymbolic.runtime.scheduler.choiceorchestration;

import java.io.Serializable;
import java.math.BigDecimal;

public class ChoiceReward implements Serializable {
    private int stepReward;
    private BigDecimal executionReward;

    public ChoiceReward() {
        this.stepReward = 0;
        this.executionReward = BigDecimal.valueOf(0);
    }

    public void addStepReward(int val) {
        this.stepReward += val;
    }

    public void addExecutionReward(BigDecimal val) {
        this.executionReward = this.executionReward.add(val);
    }

    public void addReward(ChoiceReward reward) {
        this.addStepReward(reward.stepReward);
        this.addExecutionReward(reward.executionReward);
    }

    public int compareStepReward(ChoiceReward rhs) {
        return Integer.compare(rhs.stepReward, this.stepReward);
    }

    public int compareExecutionReward(ChoiceReward rhs) {
        return rhs.executionReward.compareTo(this.executionReward);
    }

    @Override
    public String toString() {
        return String.format("(%d, %f)", stepReward, executionReward);
    }
}
