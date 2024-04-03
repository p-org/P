package pexplicit.values;

import pexplicit.runtime.machine.PMachine;

import java.util.Collection;
import java.util.List;

/**
 * Static class to compute hash values
 */
public class ComputeHash {

    /**
     * Compute hash value for a collection of PValues.
     */
    public static int getHashCode(Collection<PValue<?>> values) {
        int hashValue = 0x802CBBDB;
        for (PValue<?> val : values) {
            if (val != null) hashValue = hashValue ^ val.hashCode();
        }
        return hashValue;
    }

    /**
     * Compute hash value for a list of strings.
     */
    public static int getHashCode(List<String> keySet) {
        int hashValue = 0x802CBBDB;
        for (String val : keySet) {
            if (val != null) hashValue = hashValue ^ val.hashCode();
        }
        return hashValue;
    }

    /**
     * Compute hash value for a PMachine and an array of PValues.
     */
    public static int getHashCode(PMachine machine, PValue<?> ... values) {
        int hashValue = 0x802CBBDB;
        if (machine != null) hashValue = hashValue ^ machine.hashCode();
        for (PValue<?> val : values) {
            if (val != null) hashValue = hashValue ^ val.hashCode();
        }
        return hashValue;
    }
}
