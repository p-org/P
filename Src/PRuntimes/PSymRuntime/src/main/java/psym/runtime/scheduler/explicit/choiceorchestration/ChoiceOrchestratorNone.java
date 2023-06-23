package psym.runtime.scheduler.explicit.choiceorchestration;

import java.util.List;
import psym.valuesummary.ValueSummary;

public class ChoiceOrchestratorNone implements ChoiceOrchestrator {

    public ChoiceOrchestratorNone() {
    }

    public void reorderChoices(List<ValueSummary> choices, boolean isData) {
        // do nothing
    }
}
