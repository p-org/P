package p.runtime.values;

import lombok.var;

import java.util.Collection;
import java.util.Set;

public class ComputeHash {

    public static int getHashCode(Collection<PValue<?>> values)
    {
        int hashValue = 0x802CBBDB;
        for(var val: values)
        {
            if(val != null)
                hashValue = hashValue ^ val.hashCode();
        }
        return hashValue;
    }

    public static int getHashCode(Set<String> keySet) {
        int hashValue = 0x802CBBDB;
        for(var val: keySet)
        {
            if(val != null)
                hashValue = hashValue ^ val.hashCode();
        }
        return hashValue;
    }
}
