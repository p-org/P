package psym.runtime.scheduler.search.choiceorchestration;

import java.util.Collections;
import java.util.List;
import java.util.Random;
import psym.utils.random.RandomNumberGenerator;
import psym.valuesummary.ValueSummary;

public class ChoiceOrchestratorRandom implements ChoiceOrchestrator {

    public ChoiceOrchestratorRandom() {
    }

    public void reorderChoices(List<ValueSummary> choices, int bound, boolean isData) {
        if ((bound <= 0) || choices.size() <= bound) {
            return;
        }
        Collections.shuffle(choices, new Random(RandomNumberGenerator.getInstance().getRandomLong()));
    }
}
