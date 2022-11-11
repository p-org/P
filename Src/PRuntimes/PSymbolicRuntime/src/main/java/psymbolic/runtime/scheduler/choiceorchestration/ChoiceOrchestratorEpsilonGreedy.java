package psymbolic.runtime.scheduler.choiceorchestration;

import psymbolic.utils.RandomNumberGenerator;
import psymbolic.valuesummary.ValueSummary;

import java.util.List;

public class ChoiceOrchestratorEpsilonGreedy implements ChoiceOrchestrator {
    private static double EPSILON_MAX = 0.9;
    private static double EPSILON_MIN = 0.1;
    private static double EPSILON_DECAY_FACTOR = 0.9999;
    private static double epsilon = EPSILON_MAX;
    private ChoiceOrchestrator choiceOrchestratorExplore;
    private ChoiceOrchestrator choiceOrchestratorExploit;

    public ChoiceOrchestratorEpsilonGreedy() {
        choiceOrchestratorExplore = new ChoiceOrchestratorRandom();
        choiceOrchestratorExploit = new ChoiceOrchestratorQLearning();
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
