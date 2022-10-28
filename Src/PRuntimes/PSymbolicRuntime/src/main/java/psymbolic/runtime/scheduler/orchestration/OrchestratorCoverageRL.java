package psymbolic.runtime.scheduler.orchestration;

import psymbolic.runtime.scheduler.BacktrackTask;
import psymbolic.utils.RandomNumberGenerator;

public class OrchestratorCoverageRL implements Orchestrator {
    private static double EPSILON_MAX = 0.8;
    private static double EPSILON_MIN = 0.2;
    private static double EPSILON_DECAY_FACTOR = 0.999;
    private static double epsilon = EPSILON_MAX;
    private Orchestrator orchestratorExplore;
    private Orchestrator orchestratorExploit;
    public OrchestratorCoverageRL() {
        orchestratorExplore = new OrchestratorRandom();
        orchestratorExploit = new OrchestratorCoverageAStar();
    }

    public void addPriority(BacktrackTask task) {
        orchestratorExplore.addPriority(task);
        orchestratorExploit.addPriority(task);
    }

    public BacktrackTask getNext() {
        decayEpsilon();
        double randNum = RandomNumberGenerator.getInstance().getRandomDouble();
        if (randNum <= epsilon) {
            // explore
            return orchestratorExplore.getNext();
        } else {
            // exploit
            return orchestratorExploit.getNext();
        }
    }

    private void decayEpsilon() {
        if (epsilon > EPSILON_MIN) {
            epsilon *= EPSILON_DECAY_FACTOR;
        } else {
            epsilon = EPSILON_MIN;
        }
    }

    public void remove(BacktrackTask task) throws InterruptedException {
        orchestratorExplore.remove(task);
        orchestratorExploit.remove(task);
    }
}
