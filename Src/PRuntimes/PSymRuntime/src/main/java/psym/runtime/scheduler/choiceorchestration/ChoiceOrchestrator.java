package psym.runtime.scheduler.choiceorchestration;

import psym.valuesummary.ValueSummary;

import java.io.Serializable;
import java.util.List;

public interface ChoiceOrchestrator extends Serializable {
    void reorderChoices(List<ValueSummary> choices, boolean isData);
}
