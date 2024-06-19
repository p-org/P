package pexplicit.runtime.scheduler.choice;

import pexplicit.runtime.machine.PMachineId;
import pexplicit.runtime.scheduler.explicit.StepState;

import java.util.ArrayList;
import java.util.List;

public class ScheduleSearchUnit extends SearchUnit<PMachineId> {
    /**
     * Constructor
     */
    public ScheduleSearchUnit(List<PMachineId> u) {
        super(u);
    }

    public SearchUnit transferUnit() {
        SearchUnit newUnit = new ScheduleSearchUnit(this.unexplored);
        this.unexplored = new ArrayList<>();
        return newUnit;
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        if (unexplored != null && !unexplored.isEmpty()) {
            sb.append(String.format(" rem@%s", unexplored));
        }
        return sb.toString();
    }
}
