package psymbolic.runtime.scheduler.choiceorchestration;

import java.math.BigDecimal;

public class ChoiceReward {
    private BigDecimal value;

    public ChoiceReward() {
        this.value = BigDecimal.valueOf(0);
    }

    public void add(BigDecimal val) {
        this.value = this.value.add(val);
    }

    public void addReward(ChoiceReward reward) {
        this.add(reward.value);
    }

    public int compareTo(ChoiceReward rhs) {
        return this.value.compareTo(rhs.value);
    }

    @Override
    public String toString() {
        return String.format("%f", value);
    }
}
