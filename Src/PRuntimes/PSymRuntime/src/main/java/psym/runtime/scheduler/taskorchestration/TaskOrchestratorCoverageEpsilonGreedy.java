package psym.runtime.scheduler.taskorchestration;

import psym.runtime.scheduler.BacktrackTask;
import psym.utils.RandomNumberGenerator;

public class TaskOrchestratorCoverageEpsilonGreedy implements TaskOrchestrator {
    private static double EPSILON_MAX = 1.0;
    private static double EPSILON_MIN = 0.2;
    private static double EPSILON_DECAY_FACTOR = 0.9999;
    private static double epsilon = EPSILON_MAX;
    private TaskOrchestrator taskOrchestratorExplore;
    private TaskOrchestrator taskOrchestratorExploit;
    public TaskOrchestratorCoverageEpsilonGreedy() {
        taskOrchestratorExplore = new TaskOrchestratorRandom();
        taskOrchestratorExploit = new TaskOrchestratorCoverageAStar();
    }

    public void addPriority(BacktrackTask task) {
        taskOrchestratorExplore.addPriority(task);
        taskOrchestratorExploit.addPriority(task);
    }

    public BacktrackTask getNext() {
        decayEpsilon();
        double randNum = RandomNumberGenerator.getInstance().getRandomDouble();
        if (randNum <= epsilon) {
            // explore
            return taskOrchestratorExplore.getNext();
        } else {
            // exploit
            return taskOrchestratorExploit.getNext();
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
        taskOrchestratorExplore.remove(task);
        taskOrchestratorExploit.remove(task);
    }
}
