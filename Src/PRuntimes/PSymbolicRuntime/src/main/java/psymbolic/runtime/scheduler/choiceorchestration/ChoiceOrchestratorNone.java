package psymbolic.runtime.scheduler.choiceorchestration;

import psymbolic.valuesummary.ValueSummary;

import java.util.List;

public class ChoiceOrchestratorNone implements ChoiceOrchestrator {
    public void reorderChoices(List<ValueSummary> choices, int bound, boolean isData) {
        // do nothing
    }
}
