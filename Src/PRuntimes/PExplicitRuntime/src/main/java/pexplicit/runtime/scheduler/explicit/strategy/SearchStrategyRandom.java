package pexplicit.runtime.scheduler.explicit.strategy;

import java.util.ArrayList;
import java.util.Collections;
import java.util.HashSet;
import java.util.List;
import java.util.Random;
import java.util.Set;
import java.util.concurrent.Semaphore;

import pexplicit.utils.random.RandomNumberGenerator;

public class SearchStrategyRandom extends SearchStrategy {
    private static final Semaphore lock = new Semaphore(1);
    private static final List<SearchTask> elementList = new ArrayList<>();
    private static final Set<SearchTask> elementSet = new HashSet<>();

    public SearchStrategyRandom() {}

    public void addNewTask(SearchTask task) throws InterruptedException {
        lock.acquire();

        assert (!elementSet.contains(task));
        elementList.add(task);
        elementSet.add(task);

        lock.release();
    }

    public SearchTask popNextTask() throws InterruptedException {
        lock.acquire();

        if (elementList.isEmpty()) {
            lock.release();
            return null;
        }

        Collections.shuffle(
                elementList, new Random(RandomNumberGenerator.getInstance().getRandomLong()));
        SearchTask result = elementList.get(0);
        elementList.remove(0);
        elementSet.remove(result);

        lock.release();
        return result;
    }
}
