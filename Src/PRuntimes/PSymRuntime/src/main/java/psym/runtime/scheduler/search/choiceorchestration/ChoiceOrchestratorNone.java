package psym.runtime.scheduler.search.choiceorchestration;

import java.util.List;
import psym.valuesummary.ValueSummary;

public class ChoiceOrchestratorNone implements ChoiceOrchestrator {

    public ChoiceOrchestratorNone() {
    }

    public void reorderChoices(List<ValueSummary> choices, int bound, boolean isData) {
        // do nothing
    }
}
