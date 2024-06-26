package pexplicit.runtime.scheduler.explicit.strategy;

import java.util.ArrayList;
import java.util.Collections;
import java.util.HashSet;
import java.util.List;
import java.util.Random;
import java.util.Set;

import pexplicit.utils.random.RandomNumberGenerator;

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

    // public SearchTask popNextTaskAndCheckEmpty() {
    //     if (isEmpty)
    //         return false;
    //     else popNextTask();
    // }


}
