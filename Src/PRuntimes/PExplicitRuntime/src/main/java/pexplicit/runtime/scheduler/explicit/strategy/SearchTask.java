package pexplicit.runtime.scheduler.explicit.strategy;

import lombok.Getter;
import pexplicit.runtime.machine.PMachineId;
import pexplicit.runtime.scheduler.choice.*;
import pexplicit.values.PValue;

import java.io.Serializable;
import java.util.*;

public class SearchTask implements Serializable {
    @Getter
    private final int id;
    @Getter
    private final SearchTask parentTask;
    @Getter
    private final List<SearchTask> children = new ArrayList<>();
    @Getter
    private int currChoiceNumber = 0;
    @Getter
    private final List<Choice> prefixChoices = new ArrayList<>();
    @Getter
    private final Map<Integer, SearchUnit> searchUnits = new HashMap<>();

    public SearchTask(int id, SearchTask parentTask) {
        this.id = id;
        this.parentTask = parentTask;
    }

    public boolean isInitialTask() {
        return id == 0;
    }

    public void addChild(SearchTask task) {
        children.add(task);
    }

    public void cleanup() {
        prefixChoices.clear();
        searchUnits.clear();
    }

    public void addPrefixChoice(Choice choice) {
        prefixChoices.add(choice.copyCurrent());
    }

    public void addSuffixSearchUnit(int choiceNum, SearchUnit unit) {
        searchUnits.put(choiceNum, unit.transferUnit());
        if (choiceNum > currChoiceNumber) {
            currChoiceNumber = choiceNum;
        }
    }

    @Override
    public int hashCode() {
        return this.id;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof SearchTask)) {
            return false;
        }
        return this.id == ((SearchTask) obj).id;
    }

    @Override
    public String toString() {
        return String.format("task%d", id);
    }

    public String toStringDetailed() {
        if (isInitialTask()) {
            return String.format("%s @0::0 (parent: null)", this);
        }
        return String.format("%s ?::%d (parent: %s)",
                this,
                currChoiceNumber,
                parentTask);
    }

    public List<Integer> getSearchUnitKeys(boolean reversed) {
        List<Integer> keys = new ArrayList<>(searchUnits.keySet());
        if (reversed)
            Collections.sort(keys, Collections.reverseOrder());
        else
            Collections.sort(keys);
        return keys;
    }


    /**
     * Get the number of search units in the task
     *
     * @return Number of search units in the task
     */
    public int size() {
        return searchUnits.size();
    }

    /**
     * Get the search unit at a choice depth
     *
     * @param idx Choice depth
     * @return Search unit at depth idx
     */
    public SearchUnit getSearchUnit(int idx) {
        return searchUnits.get(idx);
    }

    /**
     * Get unexplored schedule choices at a choice depth.
     *
     * @param idx Choice depth
     * @return List of machines, or null if index is invalid
     */
    public List<PMachineId> getScheduleSearchUnit(int idx) {
        SearchUnit searchUnit = searchUnits.get(idx);
        if (searchUnit != null) {
            assert (searchUnit instanceof ScheduleSearchUnit);
            return ((ScheduleSearchUnit) searchUnit).getUnexplored();
        } else {
            return new ArrayList<>();
        }
    }

    /**
     * Get unexplored data choices at a choice depth.
     *
     * @param idx Choice depth
     * @return List of PValue, or null if index is invalid
     */
    public List<PValue<?>> getDataSearchUnit(int idx) {
        SearchUnit searchUnit = searchUnits.get(idx);
        if (searchUnit != null) {
            assert (searchUnit instanceof DataSearchUnit);
            return ((DataSearchUnit) searchUnit).getUnexplored();
        } else {
            return new ArrayList<>();
        }
    }

    /**
     * Set the schedule search unit at a choice depth.
     *
     * @param choiceNum  Choice number
     * @param unexplored List of machine to set as unexplored schedule choices
     */
    public void setScheduleSearchUnit(int choiceNum, List<PMachineId> unexplored) {
        searchUnits.put(choiceNum, new ScheduleSearchUnit(unexplored));
    }

    /**
     * Set the data search unit at a choice depth.
     *
     * @param choiceNum  Choice number
     * @param unexplored List of PValue to set as unexplored schedule choices
     */
    public void setDataSearchUnit(int choiceNum, List<PValue<?>> unexplored) {
        searchUnits.put(choiceNum, new DataSearchUnit(unexplored));
    }

    /**
     * Get the number of unexplored choices in this task
     *
     * @return Number of unexplored choices
     */
    public int getNumUnexploredChoices() {
        int numUnexplored = 0;
        for (SearchUnit<?> c : searchUnits.values()) {
            numUnexplored += c.getUnexplored().size();
        }
        return numUnexplored;
    }

    /**
     * Get the number of unexplored data choices in this task
     *
     * @return Number of unexplored data choices
     */
    public int getNumUnexploredDataChoices() {
        int numUnexplored = 0;
        for (SearchUnit<?> c : searchUnits.values()) {
            if (c instanceof DataSearchUnit) {
                numUnexplored += c.getUnexplored().size();
            }
        }
        return numUnexplored;
    }


    /**
     * Clear search unit at a choice depth
     *
     * @param choiceNum Choice depth
     */
    public void clearSearchUnit(int choiceNum) {
        searchUnits.remove(choiceNum);
    }

}
