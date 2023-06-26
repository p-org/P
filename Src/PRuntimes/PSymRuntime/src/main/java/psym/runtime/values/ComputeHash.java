package psym.runtime.values;

import java.util.Collection;
import java.util.List;

public class ComputeHash {

  public static int getHashCode(Collection<PValue<?>> values) {
    int hashValue = 0x802CBBDB;
    for (PValue<?> val : values) {
      if (val != null) hashValue = hashValue ^ val.hashCode();
    }
    return hashValue;
  }

  public static int getHashCode(List<String> keySet) {
    int hashValue = 0x802CBBDB;
    for (String val : keySet) {
      if (val != null) hashValue = hashValue ^ val.hashCode();
    }
    return hashValue;
  }
}
