package psym.runtime.scheduler.explicit.choiceorchestration;

import java.util.Collections;
import java.util.List;
import java.util.Random;
import psym.utils.random.RandomNumberGenerator;
import psym.valuesummary.ValueSummary;

public class ChoiceOrchestratorRandom implements ChoiceOrchestrator {

    public ChoiceOrchestratorRandom() {
    }

    public void reorderChoices(List<ValueSummary> choices, boolean isData) {
        Collections.shuffle(choices, new Random(RandomNumberGenerator.getInstance().getRandomLong()));
    }
}
