package psymbolic.runtime.scheduler.choiceorchestration;

import psymbolic.valuesummary.ValueSummary;

import java.io.Serializable;
import java.util.List;

public interface ChoiceOrchestrator extends Serializable {
    void reorderChoices(List<ValueSummary> choices, int bound, boolean isData);
}
