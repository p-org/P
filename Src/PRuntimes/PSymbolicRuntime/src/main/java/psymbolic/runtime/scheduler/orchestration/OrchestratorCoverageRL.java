package psymbolic.runtime.scheduler.orchestration;

import psymbolic.runtime.scheduler.BacktrackTask;
import psymbolic.utils.RandomNumberGenerator;

public class OrchestratorCoverageRL implements Orchestrator {
    private double epsilon = 1;
    private double alpha = 0.99;
    private Orchestrator orchestratorExplore;
    private Orchestrator orchestratorExploit;
    public OrchestratorCoverageRL() {
        orchestratorExplore = new OrchestratorRandom();
        orchestratorExploit = new OrchestratorCoverageAStar();
//        orchestratorExploit = new OrchestratorCoverageEstimate();
    }

    public void addPriority(BacktrackTask task) {
        orchestratorExplore.addPriority(task);
        orchestratorExploit.addPriority(task);
    }

    public BacktrackTask getNext() {
        epsilon *= alpha;
        double randNum = RandomNumberGenerator.getInstance().getRandomDouble();
        if (randNum <= epsilon) {
            // explore
            return orchestratorExplore.getNext();
        } else {
            // exploit
            return orchestratorExploit.getNext();
        }
    }

    public void remove(BacktrackTask task) throws InterruptedException {
        orchestratorExplore.remove(task);
        orchestratorExploit.remove(task);
    }
}
