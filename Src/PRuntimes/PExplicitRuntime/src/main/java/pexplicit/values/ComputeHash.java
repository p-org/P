package pexplicit.values;

import com.google.common.hash.HashCode;
import com.google.common.hash.HashFunction;
import pexplicit.runtime.machine.PMachine;

import java.nio.charset.StandardCharsets;
import java.util.Collection;
import java.util.List;
import java.util.Objects;
import java.util.SortedSet;

/**
 * Static class to compute hash values
 */
public class ComputeHash {
    public static void Initialize() {
    }


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
    public static int getHashCode(PMachine machine, PValue<?>... values) {
        int hashValue = 0x802CBBDB;
        if (machine != null) hashValue = hashValue ^ machine.hashCode();
        for (PValue<?> val : values) {
            if (val != null) hashValue = hashValue ^ val.hashCode();
        }
        return hashValue;
    }

    /**
     * Get the hash code of the protocol state using Java inbuilt hashCode() function.
     *
     * @param machines Sorted set of protocol machines
     * @return Integer representing hash code corresponding to the protocol state
     */
    public static int getHashCode(SortedSet<PMachine> machines) {
        int hashValue = 0x802CBBDB;
        for (PMachine machine : machines) {
            hashValue = hashValue ^ machine.hashCode();
            for (Object value : machine.getLocalVarValues()) {
                hashValue = hashValue ^ Objects.hashCode(value);
            }
        }
        return hashValue;
    }

    /**
     * Get the hash code of the protocol state given a hash function.
     *
     * @param machines     Sorted set of protocol machines
     * @param hashFunction Hash function to hash with
     * @return HashCode representing protocol state hashed by the given hash function
     */
    public static HashCode getHashCode(SortedSet<PMachine> machines, HashFunction hashFunction) {
        return hashFunction.hashString(getExactString(machines), StandardCharsets.UTF_8);
    }

    /**
     * Get the exact protocol state as a string.
     *
     * @param machines Sorted set of protocol machines
     * @return String representing the exact protocol state
     */
    public static String getExactString(SortedSet<PMachine> machines) {
        StringBuilder sb = new StringBuilder();
        for (PMachine machine : machines) {
            sb.append(machine);
            for (Object value : machine.getLocalVarValues()) {
                sb.append(value);
            }
        }
        return sb.toString();
    }

}
