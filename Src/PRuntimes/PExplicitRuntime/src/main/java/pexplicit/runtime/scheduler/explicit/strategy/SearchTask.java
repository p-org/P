package pexplicit.runtime.scheduler.explicit.strategy;

import lombok.Getter;
import pexplicit.runtime.scheduler.choice.Choice;
import pexplicit.runtime.scheduler.choice.DataChoice;
import pexplicit.runtime.scheduler.choice.ScheduleChoice;

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
    private final List<Choice> prefixChoices = new ArrayList<>();
    private final List<Choice> suffixChoices = new ArrayList<>();
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
        prefixChoices.clear();
        suffixChoices.clear();
    }

    public void addPrefixChoice(Choice choice) {
        prefixChoices.add(choice.copyCurrent());
    }

    public void addSuffixChoice(Choice choice) {
        if (choice instanceof ScheduleChoice scheduleChoice) {
            numUnexploredScheduleChoices += scheduleChoice.getUnexplored().size();
        } else {
            numUnexploredDataChoices += ((DataChoice) choice).getUnexplored().size();
        }
        suffixChoices.add(choice.transferChoice());
    }

    public List<Choice> getAllChoices() {
        List<Choice> result = new ArrayList<>(prefixChoices);
        result.addAll(suffixChoices);
        assert (result.size() == (currChoiceNumber + suffixChoices.size()));
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
                suffixChoices.get(0).getStepNumber(),
                currChoiceNumber,
                parentTask);
    }
}
