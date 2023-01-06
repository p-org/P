package psym.runtime.scheduler.choiceorchestration;

import psym.utils.RandomNumberGenerator;
import psym.valuesummary.ValueSummary;

import java.util.Collections;
import java.util.List;
import java.util.Random;

public class ChoiceOrchestratorRandom implements ChoiceOrchestrator {

    public ChoiceOrchestratorRandom() {}

    public void reorderChoices(List<ValueSummary> choices, int bound, boolean isData) {
        if ((bound <= 0) || choices.size() <= bound) {
            return;
        }
        Collections.shuffle(choices, new Random(RandomNumberGenerator.getInstance().getRandomLong()));
    }
}
