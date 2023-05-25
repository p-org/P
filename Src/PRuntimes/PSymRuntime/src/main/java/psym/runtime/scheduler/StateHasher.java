package psym.runtime.scheduler;

import java.util.*;

public class StateHasher {

    Map<String, Map<Integer, List<Map<Object, Integer>>>> explored = new HashMap<>();

    /** Clear state hasher
     */
    public void clear() {
        explored.clear();
    }

    /**
     * Check if a machine state is present, or else, add it to the state hasher
     * @param machineType Machine type
     * @param machineId Machine id
     * @param varIdx Machine variable index
     * @param varValue Machine variable value at varIdx
     * @return true iff machine state is newly added
     */
    public int getVarHash(String machineType, int machineId, int varIdx, Object varValue) {
        Map<Integer, List<Map<Object, Integer>>> typeEntry = explored.get(machineType);
        if (typeEntry == null) {
            explored.put(machineType, new HashMap<>());
            typeEntry = explored.get(machineType);
        }
        List<Map<Object, Integer>> idEntry = typeEntry.get(machineId);
        if (idEntry == null) {
            typeEntry.put(machineId, new ArrayList<>());
            idEntry = typeEntry.get(machineId);
        }
        while (idEntry.size() <= varIdx) {
            idEntry.add(new HashMap<>());
        }
        Map<Object, Integer> varEntry = idEntry.get(varIdx);
        Integer varHash = varEntry.get(varValue);
        if (varHash == null) {
            varEntry.put(varValue, varEntry.size());
            varHash = varEntry.get(varValue);
        }
        return varHash;
    }
}
