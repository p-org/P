package pexplicit.runtime.scheduler.explicit.strategy;

import lombok.Getter;
import pexplicit.runtime.scheduler.choice.DataSearchUnit;
import pexplicit.runtime.scheduler.choice.SearchUnit;
import pexplicit.runtime.scheduler.choice.ScheduleSearchUnit;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.List;

public class SearchTask implements Serializable {
    @Getter
    private final int id;
    @Getter
    private final SearchTask parentTask;
    @Getter
    private final List<SearchTask> children = new ArrayList<>();
    @Getter
    private final int currChoiceNumber;
    private final List<SearchUnit> prefixSearchUnits = new ArrayList<>();
    private final List<SearchUnit> suffixSearchUnits = new ArrayList<>();
    @Getter
    private int numUnexploredScheduleChoices = 0;
    @Getter
    private int numUnexploredDataChoices = 0;

    public SearchTask(int id, int choiceNum, SearchTask parentTask) {
        this.id = id;
        this.currChoiceNumber = choiceNum;
        this.parentTask = parentTask;
    }

    public boolean isInitialTask() {
        return id == 0;
    }

    public void addChild(SearchTask task) {
        children.add(task);
    }

    public void cleanup() {
        prefixSearchUnits.clear();
        suffixSearchUnits.clear();
    }

    public void addPrefixChoice(SearchUnit searchUnit) {
        prefixSearchUnits.add(searchUnit.copyCurrent());
    }

    public void addSuffixChoice(SearchUnit searchUnit) {
        if (searchUnit instanceof ScheduleSearchUnit scheduleChoice) {
            numUnexploredScheduleChoices += scheduleChoice.getUnexplored().size();
        } else {
            numUnexploredDataChoices += ((DataSearchUnit) searchUnit).getUnexplored().size();
        }
        suffixSearchUnits.add(searchUnit.transferChoice());
    }

    public List<SearchUnit> getAllChoices() {
        List<SearchUnit> result = new ArrayList<>(prefixSearchUnits);
        result.addAll(suffixSearchUnits);
        assert (result.size() == (currChoiceNumber + suffixSearchUnits.size()));
        return result;
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
        return String.format("%s @%d::%d (parent: %s)",
                this,
                suffixSearchUnits.get(0).getStepNumber(),
                currChoiceNumber,
                parentTask);
    }
}
