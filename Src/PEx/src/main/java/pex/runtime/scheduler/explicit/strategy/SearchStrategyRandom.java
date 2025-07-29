package pex.runtime.scheduler.explicit.strategy;

import pex.runtime.PExGlobal;
import pex.utils.random.RandomNumberGenerator;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Random;

public class SearchStrategyRandom extends SearchStrategy {
    private static final List<SearchTask> elementList = new ArrayList<>();

    public SearchStrategyRandom() {
    }

    public void addTask(SearchTask task) {
        PExGlobal.getPendingTasks().add(task);
        elementList.add(task);
    }

    public SearchTask popTask() {
        assert (!elementList.isEmpty());
        Collections.shuffle(
                elementList, new Random(RandomNumberGenerator.getInstance().getRandomLong()));
        SearchTask result = elementList.get(0);
        elementList.remove(0);
        PExGlobal.getPendingTasks().remove(result);
        return result;
    }
}
