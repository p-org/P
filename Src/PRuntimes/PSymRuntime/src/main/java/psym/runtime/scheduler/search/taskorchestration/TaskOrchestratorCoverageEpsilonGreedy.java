package psym.runtime.scheduler.search.taskorchestration;

import lombok.Setter;
import psym.utils.random.RandomNumberGenerator;

public class TaskOrchestratorCoverageEpsilonGreedy implements TaskOrchestrator {
  private static final double EPSILON_MAX = 1.0;
  private static final double EPSILON_MIN = 0.3;
  @Setter private static double EPSILON_DECAY_FACTOR = 0.999;
  private static double epsilon = EPSILON_MAX;
  private final TaskOrchestrator taskOrchestratorExplore;
  private final TaskOrchestrator taskOrchestratorExploit;

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
