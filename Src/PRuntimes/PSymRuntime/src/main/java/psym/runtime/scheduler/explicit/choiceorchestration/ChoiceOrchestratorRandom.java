package psym.runtime.scheduler.explicit.choiceorchestration;

import psym.utils.RandomNumberGenerator;
import psym.valuesummary.ValueSummary;

import java.util.Collections;
import java.util.List;
import java.util.Random;

public class ChoiceOrchestratorRandom implements ChoiceOrchestrator {

    public ChoiceOrchestratorRandom() {
    }

    public void reorderChoices(List<ValueSummary> choices, boolean isData) {
        Collections.shuffle(choices, new Random(RandomNumberGenerator.getInstance().getRandomLong()));
    }
}
