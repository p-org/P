package psym.runtime.scheduler.choiceorchestration;

import psym.utils.GlobalData;
import psym.utils.RandomNumberGenerator;
import psym.valuesummary.ValueSummary;

import java.util.Collections;
import java.util.List;
import java.util.Random;

public class ChoiceOrchestratorQLearning implements ChoiceOrchestrator {
    public ChoiceOrchestratorQLearning() {
    }

    public void reorderChoices(List<ValueSummary> choices, boolean isData) {
        Collections.shuffle(choices, new Random(RandomNumberGenerator.getInstance().getRandomLong()));
        Collections.sort(choices, GlobalData.getChoiceLearningStats().getChoiceComparator());
    }
}
