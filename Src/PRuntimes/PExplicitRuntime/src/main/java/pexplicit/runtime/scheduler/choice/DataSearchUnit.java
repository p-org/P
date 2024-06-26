package pexplicit.runtime.scheduler.choice;

import pexplicit.values.PValue;

import java.util.ArrayList;
import java.util.List;

public class DataSearchUnit extends SearchUnit<PValue<?>> {
    /**
     * Constructor
     */
    public DataSearchUnit(List<PValue<?>> u) {
        super(u);
    }

    public SearchUnit transferUnit() {
        SearchUnit newUnit = new DataSearchUnit(this.unexplored);
        this.unexplored = new ArrayList<>();
        return newUnit;
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        if (unexplored != null && !unexplored.isEmpty()) {
            sb.append(String.format(" rem:%s", unexplored));
        }
        return sb.toString();
    }
}
