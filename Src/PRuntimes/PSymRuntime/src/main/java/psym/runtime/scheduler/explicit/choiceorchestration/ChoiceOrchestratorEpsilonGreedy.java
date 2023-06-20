package psym.runtime.scheduler.explicit.choiceorchestration;

import lombok.Setter;
import psym.utils.random.RandomNumberGenerator;
import psym.valuesummary.ValueSummary;

import java.util.List;

public class ChoiceOrchestratorEpsilonGreedy implements ChoiceOrchestrator {
    private static final double EPSILON_MAX = 1.0;
    private static final double EPSILON_MIN = 0.3;

    @Setter
    private static double EPSILON_DECAY_FACTOR = 0.9999;
    private static double epsilon = EPSILON_MAX;
    private final ChoiceOrchestrator choiceOrchestratorExplore;
    private final ChoiceOrchestrator choiceOrchestratorExploit;

    public ChoiceOrchestratorEpsilonGreedy() {
        choiceOrchestratorExplore = new ChoiceOrchestratorRandom();
        choiceOrchestratorExploit = new ChoiceOrchestratorQLearning();
    }

    public void reorderChoices(List<ValueSummary> choices, boolean isData) {
        decayEpsilon();
        double randNum = RandomNumberGenerator.getInstance().getRandomDouble();
        if (randNum <= epsilon) {
            // explore
            choiceOrchestratorExplore.reorderChoices(choices, isData);
        } else {
            // exploit
            choiceOrchestratorExploit.reorderChoices(choices, isData);
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
