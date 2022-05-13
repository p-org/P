package psymbolic.runtime;

import java.io.Serializable;
import java.util.HashMap;
import java.util.Map;

/** Represent a P enum type */
public class Enum implements Serializable {
    /** Name of the enum type */
    private final String name;
    /** Map from enum values to Integers */
    private final Map<String, Integer> stringToName;

    /** Make a new Enum type
     * @param name Name of the enum
     * @param enums Possible enum values
     */
    public Enum(String name, String ... enums) {
        this.name = name;
        this.stringToName = new HashMap<>();
        for (int i = 0; i < enums.length; i++) {
            stringToName.put(enums[i], i);
        }
    }

    /** Convert enum value to int
     * @param name enum value
     * @return enum value integer
     */
    private int getInt(String name) {
        return this.stringToName.get(name);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj instanceof Enum) {
            Enum e = (Enum) obj;
            return this.name.equals(e.name) && this.stringToName.equals(e.stringToName);
        }
        return false;
    }

    @Override
    public int hashCode() {
        int hash = 17;
        hash = hash * 31 + name.hashCode();
        hash = hash * 31 + stringToName.hashCode();
        return hash;
    }
}
