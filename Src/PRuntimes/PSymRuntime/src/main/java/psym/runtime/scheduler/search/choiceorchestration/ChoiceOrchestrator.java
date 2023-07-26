package psym.runtime.scheduler.search.choiceorchestration;

import java.io.Serializable;
import java.util.List;
import psym.valuesummary.ValueSummary;

public interface ChoiceOrchestrator extends Serializable {
    void reorderChoices(List<ValueSummary> choices, int bound, boolean isData);
}
