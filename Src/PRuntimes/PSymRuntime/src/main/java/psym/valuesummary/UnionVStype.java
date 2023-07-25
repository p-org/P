package psym.valuesummary;

import java.io.Serializable;
import java.util.Arrays;
import java.util.HashMap;

public class UnionVStype implements Serializable {
  private static final HashMap<String, UnionVStype> allTypes = new HashMap<>();

  final Class<? extends ValueSummary> typeClass;
  final String[] names;

  private UnionVStype(Class<? extends ValueSummary> tc, String[] n) {
    typeClass = tc;
    names = n;
  }

  public static UnionVStype getUnionVStype(Class<? extends ValueSummary> tc, String[] n) {
    UnionVStype result;

    String typeName = tc.toString();
    if (n != null) {
      typeName += String.format("[%s]", String.join(",", n));
    }

    if (!allTypes.containsKey(typeName)) {
      result = new UnionVStype(tc, n);
      allTypes.put(typeName, result);
    } else {
      result = allTypes.get(typeName);
    }

    return result;
  }

  @Override
  public String toString() {
    String out = "[type: " + typeClass + ", " + "names: " + names + "]";
    return out;
  }

  @Override
  public boolean equals(Object o) {
    if (this == o) return true;
    if (!(o instanceof UnionVStype)) return false;
    UnionVStype rhs = (UnionVStype) o;
    if (names == null) {
      return (rhs.names == null) && typeClass.equals(rhs.typeClass);
    } else if (rhs.names == null) {
      return false;
    } else {
      return typeClass.equals(rhs.typeClass) && (Arrays.equals(names, rhs.names));
    }
  }

  @Override
  public int hashCode() {
    if (names == null) {
      return typeClass.hashCode();
    } else {
      return 31 * typeClass.hashCode() + Arrays.hashCode(names);
    }
  }
}
