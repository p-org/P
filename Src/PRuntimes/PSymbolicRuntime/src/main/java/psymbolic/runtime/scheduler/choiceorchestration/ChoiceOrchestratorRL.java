package psymbolic.runtime.scheduler.choiceorchestration;

import psymbolic.utils.RandomNumberGenerator;
import psymbolic.valuesummary.ValueSummary;

import java.util.List;

public class ChoiceOrchestratorRL implements ChoiceOrchestrator {
    private static double EPSILON_MAX = 0.8;
    private static double EPSILON_MIN = 0.2;
    private static double EPSILON_DECAY_FACTOR = 0.999;
    private static double epsilon = EPSILON_MAX;
    private ChoiceOrchestrator choiceOrchestratorExplore;
    private ChoiceOrchestrator choiceOrchestratorExploit;

    public ChoiceOrchestratorRL() {
        choiceOrchestratorExplore = new ChoiceOrchestratorRandom();
        choiceOrchestratorExploit = new ChoiceOrchestratorEstimate();
    }

    public void reorderChoices(List<ValueSummary> choices, int bound, boolean isData) {
        decayEpsilon();
        double randNum = RandomNumberGenerator.getInstance().getRandomDouble();
        if (randNum <= epsilon) {
            // explore
            choiceOrchestratorExplore.reorderChoices(choices, bound, isData);
        } else {
            // exploit
            choiceOrchestratorExploit.reorderChoices(choices, bound, isData);
        }
    }

    private void decayEpsilon() {
        if (epsilon > EPSILON_MIN) {
            epsilon *= EPSILON_DECAY_FACTOR;
        } else {
            epsilon = EPSILON_MIN;
        }
    }
}
