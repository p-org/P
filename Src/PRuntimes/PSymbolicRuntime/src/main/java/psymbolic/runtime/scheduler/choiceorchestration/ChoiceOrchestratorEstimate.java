package psymbolic.runtime.scheduler.choiceorchestration;

import psymbolic.utils.GlobalData;
import psymbolic.utils.RandomNumberGenerator;
import psymbolic.valuesummary.PrimitiveVS;
import psymbolic.valuesummary.ValueSummary;

import java.io.Serializable;
import java.util.*;

public class ChoiceOrchestratorEstimate implements ChoiceOrchestrator {
    public ChoiceOrchestratorEstimate() {}

    private class ChoiceComparator implements Comparator<ValueSummary>, Serializable {
        private Map<ValueSummary, ChoiceReward> sortOrder;

        public ChoiceComparator(Map<ValueSummary, ChoiceReward> sortOrder) {
            this.sortOrder = sortOrder;
        }

        @Override
        public int compare(ValueSummary lhs, ValueSummary rhs) {
            ChoiceReward lhsReward = sortOrder.get(lhs);
            ChoiceReward rhsReward = sortOrder.get(rhs);
            assert(lhsReward != null);
            assert(rhsReward != null);

            return lhsReward.compareStepReward(rhsReward);
        }
    }

    public void reorderChoices(List<ValueSummary> choices, int bound, boolean isData) {
        if ((bound <= 0) || choices.size() <= bound) {
            return;
        }
        Collections.shuffle(choices, new Random(RandomNumberGenerator.getInstance().getRandomLong()));
        if (isData) {
            reorderDataChoices(choices, bound);
        } else {
            reorderScheduleChoices(choices, bound);
        }
    }

    private void reorderScheduleChoices(List<ValueSummary> choices, int bound) {
        Map<ValueSummary, ChoiceReward> choiceToReward = new HashMap<>();
        for (ValueSummary choice: choices) {
            assert(choice instanceof PrimitiveVS);
            choiceToReward.put(choice, GlobalData.getChoiceFeatureStats().getChoiceRewardCumulative((PrimitiveVS) choice));
        }
        Collections.sort(choices, new ChoiceComparator(choiceToReward));
    }

    private void reorderDataChoices(List<ValueSummary> choices, int bound) {
        // TODO
    }
}
