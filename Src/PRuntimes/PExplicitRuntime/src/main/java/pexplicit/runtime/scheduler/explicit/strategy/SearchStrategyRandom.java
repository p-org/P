package pexplicit.runtime.scheduler.explicit.strategy;

import pexplicit.utils.random.RandomNumberGenerator;

import java.util.*;

public class SearchStrategyRandom extends SearchStrategy {
    private final List<SearchTask> elementList;
    private final Set<SearchTask> elementSet;

    public SearchStrategyRandom() {
        elementList = new ArrayList<>();
        elementSet = new HashSet<>();
    }

    public void addNewTask(SearchTask task) {
        assert (!elementSet.contains(task));
        elementList.add(task);
        elementSet.add(task);
    }

    public SearchTask popNextTask() {
        assert (!elementList.isEmpty());
        Collections.shuffle(
                elementList, new Random(RandomNumberGenerator.getInstance().getRandomLong()));
        SearchTask result = elementList.get(0);
        elementList.remove(0);
        elementSet.remove(result);
        return result;
    }
}
