package psymbolic.runtime.scheduler.choiceorchestration;

import psymbolic.utils.GlobalData;
import psymbolic.utils.RandomNumberGenerator;
import psymbolic.valuesummary.ValueSummary;

import java.util.*;

public class ChoiceOrchestratorQLearning implements ChoiceOrchestrator {
    public ChoiceOrchestratorQLearning() {}

    public void reorderChoices(List<ValueSummary> choices, int bound, boolean isData) {
        if ((bound <= 0) || choices.size() <= bound) {
            return;
        }
        Collections.shuffle(choices, new Random(RandomNumberGenerator.getInstance().getRandomLong()));
        Collections.sort(choices, GlobalData.getChoiceLearningStats().getChoiceComparator());
    }
}
