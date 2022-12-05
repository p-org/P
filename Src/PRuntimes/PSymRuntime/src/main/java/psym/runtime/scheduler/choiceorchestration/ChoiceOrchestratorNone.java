package psym.runtime.scheduler.choiceorchestration;

import psym.valuesummary.ValueSummary;

import java.util.List;

public class ChoiceOrchestratorNone implements ChoiceOrchestrator {

    public ChoiceOrchestratorNone() {}

    public void reorderChoices(List<ValueSummary> choices, int bound, boolean isData) {
        // do nothing
    }
}
