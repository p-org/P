package psym.utils;

import java.util.List;
import java.util.stream.Collectors;
import lombok.Getter;
import psym.utils.exception.BugFoundException;
import psym.utils.exception.LivenessException;
import psym.valuesummary.Guard;
import psym.valuesummary.GuardedValue;
import psym.valuesummary.PrimitiveVS;

public class Assert {
  @Getter
  private static String failureType = "";
  @Getter
  private static String failureMsg = "";

  public static void prop(boolean p, String msg, Guard pc) {
    if (!p) {
      failureType = "prop";
      failureMsg = "Property violated: " + msg;
      throw new BugFoundException(failureMsg, pc);
    }
  }

  public static void progProp(boolean p, PrimitiveVS<String> msg, Guard pc) {
    if (!p) {
      failureType = "progProp";
      List<String> msgs =
          msg.restrict(pc).getGuardedValues().stream()
              .map(GuardedValue::getValue)
              .collect(Collectors.toList());
      failureMsg = "Properties violated: " + msgs;
      throw new BugFoundException(failureMsg, pc);
    }
  }

  public static void liveness(boolean p, String msg, Guard pc) {
    if (!p) {
      failureType = "liveness";
      failureMsg = "Property violated: " + msg;
      throw new LivenessException(failureMsg, pc);
    }
  }

  public static void cycle(boolean p, String msg, Guard pc) {
    if (!p) {
      failureType = "cycle";
      failureMsg = "Property violated: " + msg;
      throw new LivenessException(failureMsg, pc);
    }
  }
}
