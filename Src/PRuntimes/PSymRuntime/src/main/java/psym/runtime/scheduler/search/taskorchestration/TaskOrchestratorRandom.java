package psym.runtime.scheduler.search.taskorchestration;

import java.util.*;
import psym.utils.random.RandomNumberGenerator;

public class TaskOrchestratorRandom implements TaskOrchestrator {
  private final List<BacktrackTask> elementList;
  private final Set<BacktrackTask> elementSet;

  public TaskOrchestratorRandom() {
    elementList = new ArrayList<>();
    elementSet = new HashSet<>();
  }

  public void addPriority(BacktrackTask task) {
    if (elementSet.contains(task)) {
      // do nothing
    } else {
      elementList.add(task);
      elementSet.add(task);
    }
  }

  public BacktrackTask getNext() {
    assert (!elementList.isEmpty());
    Collections.shuffle(
        elementList, new Random(RandomNumberGenerator.getInstance().getRandomLong()));
    return elementList.get(RandomNumberGenerator.getInstance().getRandomInt(elementList.size()));
  }

  public void remove(BacktrackTask task) {
    elementList.remove(task);
    elementSet.remove(task);
  }
}
