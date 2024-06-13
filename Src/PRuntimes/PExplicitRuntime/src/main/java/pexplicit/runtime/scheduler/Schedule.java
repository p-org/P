package pexplicit.runtime.scheduler;

import lombok.Data;
import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.machine.PMachineId;
import pexplicit.runtime.scheduler.choice.*;
import pexplicit.runtime.scheduler.explicit.StepState;
import pexplicit.values.PValue;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.List;

/**
 * Represents a single (possibly partial) schedule.
 */
public class Schedule implements Serializable {
    /**
     * List of current choices
     */
    @Getter
    @Setter
    private List<SearchUnit> searchUnits = new ArrayList<>();

    /**
     * Step state at the start of a scheduler step.
     * Used in stateful backtracking
     */
    @Setter
    private transient StepState stepBeginState = null;

    /**
     * Constructor
     */
    public Schedule() {
    }

    /**
     * Get the choice at a choice depth
     *
     * @param idx Choice depth
     * @return Choice at depth idx
     */
    public SearchUnit getChoice(int idx) {
        return searchUnits.get(idx);
    }

    /**
     * Clear choice at a choice depth
     *
     * @param idx Choice depth
     */
    public void clearChoice(int idx) {
        searchUnits.get(idx).clearCurrent();
        searchUnits.get(idx).clearUnexplored();
    }

    /**
     * Remove choices after a choice depth
     *
     * @param choiceNum Choice depth
     */
    public void removeChoicesAfter(int choiceNum) {
        searchUnits.subList(choiceNum + 1, searchUnits.size()).clear();
    }

    /**
     * Get the number of unexplored choices in this schedule
     *
     * @return Number of unexplored choices
     */
    public int getNumUnexploredChoices() {
        int numUnexplored = 0;
        for (SearchUnit<?> c : searchUnits) {
            numUnexplored += c.getUnexplored().size();
        }
        return numUnexplored;
    }

    /**
     * Get the number of unexplored data choices in this schedule
     *
     * @return Number of unexplored data choices
     */
    public int getNumUnexploredDataChoices() {
        int numUnexplored = 0;
        for (SearchUnit<?> c : searchUnits) {
            if (c instanceof DataSearchUnit) {
                numUnexplored += c.getUnexplored().size();
            }
        }
        return numUnexplored;
    }

    /**
     * Set the schedule choice at a choice depth.
     *
     * @param stepNum    Step number
     * @param choiceNum  Choice number
     * @param current    Machine to set as current schedule choice
     * @param unexplored List of machine to set as unexplored schedule choices
     */
    public void setScheduleChoice(int stepNum, int choiceNum, ScheduleChoice current, List<ScheduleChoice> unexplored) {
        if (choiceNum == searchUnits.size()) {
            searchUnits.add(null);
        }
        assert (choiceNum < searchUnits.size());
        if (PExplicitGlobal.getConfig().isStatefulBacktrackEnabled()
                && stepNum != 0) {
            assert (stepBeginState != null);
            searchUnits.set(choiceNum, new ScheduleSearchUnit(stepNum, choiceNum, current, unexplored, stepBeginState));
        } else {
            searchUnits.set(choiceNum, new ScheduleSearchUnit(stepNum, choiceNum, current, unexplored, null));
        }
    }

    /**
     * Set the data choice at a choice depth.
     *
     * @param stepNum    Step number
     * @param choiceNum  Choice number
     * @param current    PValue to set as current schedule choice
     * @param unexplored List of PValue to set as unexplored schedule choices
     */
    public void setDataChoice(int stepNum, int choiceNum, DataChoice current, List<DataChoice> unexplored) {
        if (choiceNum == searchUnits.size()) {
            searchUnits.add(null);
        }
        assert (choiceNum < searchUnits.size());
        searchUnits.set(choiceNum, new DataSearchUnit(stepNum, choiceNum, current, unexplored));
    }

    /**
     * Get the current schedule choice at a choice depth.
     *
     * @param idx Choice depth
     * @return Current schedule choice
     */
    public ScheduleChoice getCurrentScheduleChoice(int idx) {
        assert (searchUnits.get(idx) instanceof ScheduleSearchUnit);
        return ((ScheduleSearchUnit) searchUnits.get(idx)).getCurrent();
    }

    /**
     * Get the current data choice at a choice depth.
     *
     * @param idx Choice depth
     * @return Current data choice
     */
    public DataChoice getCurrentDataChoice(int idx) {
        assert (searchUnits.get(idx) instanceof DataSearchUnit);
        return ((DataSearchUnit) searchUnits.get(idx)).getCurrent();
    }

    /**
     * Get unexplored schedule choices at a choice depth.
     *
     * @param idx Choice depth
     * @return List of machines, or null if index is invalid
     */
    public List<ScheduleChoice> getUnexploredScheduleChoices(int idx) {
        if (idx < size()) {
            assert (searchUnits.get(idx) instanceof ScheduleSearchUnit);
            return ((ScheduleSearchUnit) searchUnits.get(idx)).getUnexplored();
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
    public List<DataChoice> getUnexploredDataChoices(int idx) {
        if (idx < size()) {
            assert (searchUnits.get(idx) instanceof DataSearchUnit);
            return ((DataSearchUnit) searchUnits.get(idx)).getUnexplored();
        } else {
            return new ArrayList<>();
        }
    }

    public ScheduleSearchUnit getScheduleChoiceAt(SearchUnit searchUnit) {
        if (searchUnit instanceof ScheduleSearchUnit scheduleChoice) {
            return scheduleChoice;
        } else {
            for (int i = searchUnit.getChoiceNumber() - 1; i >= 0; i--) {
                SearchUnit c = searchUnits.get(i);
                if (c instanceof ScheduleSearchUnit scheduleChoice) {
                    return scheduleChoice;
                }
            }
        }
        return null;
    }

    /**
     * Clear current choices at a choice depth
     *
     * @param idx Choice depth
     */
    public void clearCurrent(int idx) {
        searchUnits.get(idx).clearCurrent();
    }

    /**
     * Get the number of choices in the schedule
     *
     * @return Number of choices in the schedule
     */
    public int size() {
        return searchUnits.size();
    }
}
