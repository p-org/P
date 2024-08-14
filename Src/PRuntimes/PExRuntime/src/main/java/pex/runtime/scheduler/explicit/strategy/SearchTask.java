package pex.runtime.scheduler.explicit.strategy;

import lombok.Getter;
import pex.runtime.PExGlobal;
import pex.runtime.logger.PExLogger;
import pex.runtime.machine.PMachineId;
import pex.runtime.scheduler.choice.Choice;
import pex.runtime.scheduler.choice.DataSearchUnit;
import pex.runtime.scheduler.choice.ScheduleSearchUnit;
import pex.runtime.scheduler.choice.SearchUnit;
import pex.runtime.scheduler.explicit.StatefulBacktrackingMode;
import pex.values.PValue;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.Paths;
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
    private int totalUnexploredChoices = 0;
    @Getter
    private int totalUnexploredDataChoices = 0;

    @Getter
    private List<Choice> prefixChoices = new ArrayList<>();
    @Getter
    private Map<Integer, SearchUnit> searchUnits = new HashMap<>();
    @Getter
    private String serializeFile = null;

    public SearchTask(int id, SearchTask parentTask) {
        this.id = id;
        this.parentTask = parentTask;
    }

    public static void Initialize() {
        String taskPath = PExGlobal.getConfig().getOutputFolder() + "/tasks/";
        try {
            Files.createDirectories(Paths.get(taskPath));
        } catch (IOException e) {
            throw new RuntimeException("Failed to initialize tasks at " + taskPath, e);
        }
    }

    public static void Cleanup() {
        String taskPath = PExGlobal.getConfig().getOutputFolder() + "/tasks/";
        File taskDir = new File(taskPath);
        String[] entries = taskDir.list();
        for (String s : entries) {
            File currentFile = new File(taskDir.getPath(), s);
            currentFile.delete();
        }
        taskDir.delete();
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
        boolean copyState = (PExGlobal.getConfig().getStatefulBacktrackingMode() == StatefulBacktrackingMode.All);
        prefixChoices.add(choice.copyCurrent(copyState));
    }

    public void addSuffixSearchUnit(int choiceNum, SearchUnit unit) {
        totalUnexploredChoices += unit.getUnexplored().size();
        if (unit instanceof DataSearchUnit) {
            totalUnexploredDataChoices += unit.getUnexplored().size();
        }

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
    public int getCurrentNumUnexploredChoices() {
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
    public int getCurrentNumUnexploredDataChoices() {
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

    public void writeToFile() {
        assert (serializeFile == null);
        assert (prefixChoices != null);
        assert (searchUnits != null);

        serializeFile = PExGlobal.getConfig().getOutputFolder() + "/tasks/" + this + ".ser";
        try {
            FileOutputStream fos = new FileOutputStream(serializeFile);
            ObjectOutputStream oos = new ObjectOutputStream(fos);
            oos.writeObject(this.prefixChoices);
            oos.writeObject(this.searchUnits);
            long szBytes = Files.size(Paths.get(serializeFile));
            PExLogger.logSerializeTask(this, szBytes);
        } catch (IOException e) {
            throw new RuntimeException("Failed to write task in file " + serializeFile, e);
        }

        prefixChoices = null;
        searchUnits = null;
    }

    public void readFromFile() {
        assert (serializeFile != null);
        assert (prefixChoices == null);
        assert (searchUnits == null);

        try {
            PExLogger.logDeserializeTask(this);
            FileInputStream fis;
            fis = new FileInputStream(serializeFile);
            ObjectInputStream ois = new ObjectInputStream(fis);
            prefixChoices = (ArrayList<Choice>) ois.readObject();
            searchUnits = (HashMap<Integer, SearchUnit>) ois.readObject();
            Files.delete(Paths.get(serializeFile));
        } catch (IOException | ClassNotFoundException e) {
            throw new RuntimeException("Failed to read task from file " + serializeFile, e);
        }

        serializeFile = null;
    }

}
