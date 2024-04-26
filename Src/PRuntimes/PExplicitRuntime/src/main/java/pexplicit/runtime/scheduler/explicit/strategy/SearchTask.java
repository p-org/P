package pexplicit.runtime.scheduler.explicit.strategy;

import lombok.Getter;
import pexplicit.runtime.scheduler.Choice;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class SearchTask implements Serializable {
    @Getter
    private final int id;
    @Getter
    private final SearchTask parentTask;
    @Getter
    private final List<SearchTask> children = new ArrayList<>();
    @Getter
    private final int currChoiceNumber;
    private final Choice currChoice;
    @Getter
    private int numUnexploredScheduleChoices = 0;
    @Getter
    private int numUnexploredDataChoices = 0;
    private final Map<Integer, Choice> prefixChoices = new HashMap<>();
    private final List<Choice> suffixChoices = new ArrayList<>();

    public SearchTask(int id, Choice choice, int choiceNum, SearchTask parentTask) {
        this.id = id;
        this.currChoice = choice;
        this.currChoiceNumber = choiceNum;
        this.parentTask = parentTask;
        if (!isInitialTask()) {
            numUnexploredScheduleChoices += choice.getUnexploredScheduleChoices().size();
            numUnexploredDataChoices += choice.getUnexploredDataChoices().size();
        }
    }

    public boolean isInitialTask() {
        return id == 0;
    }

    public void addChild(SearchTask task) {
        children.add(task);
    }

    public void cleanup() {
        if (currChoice != null) {
            currChoice.clearUnexplored();
        }
        suffixChoices.clear();
    }

    public void addPrefixChoice(Choice choice, int choiceNum) {
        assert (!choice.isUnexploredNonEmpty());
        prefixChoices.put(choiceNum, choice);
    }

    public void addSuffixChoice(Choice choice) {
        // TODO: check if we need copy here
        suffixChoices.add(choice);
        numUnexploredScheduleChoices += choice.getUnexploredScheduleChoices().size();
        numUnexploredDataChoices += choice.getUnexploredDataChoices().size();
    }

    public List<Choice> getAllChoices() {
        List<Choice> result = new ArrayList<>(suffixChoices);
        result.add(0, currChoice);

        SearchTask task = this;
        int i = currChoiceNumber - 1;
        while (i >= 0) {
            Choice c = task.prefixChoices.get(i);
            if (c == null) {
                assert (!task.isInitialTask());
                task = task.parentTask;
            } else {
                result.add(0, c);
                i--;
            }
        }
        assert (result.size() == (currChoiceNumber + 1 + suffixChoices.size()));
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
        if (currChoice.getChoiceStep() != null) {
            return String.format("%s @%d::%d (parent: %s)",
                    this,
                    currChoice.getChoiceStep().getStepNumber(),
                    currChoiceNumber,
                    parentTask);
        }
        return String.format("%s @?::%d (parent: %s)",
                this,
                currChoiceNumber,
                parentTask);
    }
}
