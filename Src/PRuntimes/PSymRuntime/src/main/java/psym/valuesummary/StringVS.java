package psym.valuesummary;

import static psym.runtime.Concretizer.getConcreteValues;

import java.text.MessageFormat;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import psym.runtime.Concretizer;

public class StringVS {

  public static PrimitiveVS<String> formattedStringVS(
      Guard pc, String baseString, ValueSummary... args) {
    final Map<String, Guard> results = new HashMap<>();
    if (args.length == 0) {
      results.merge(baseString, pc, Guard::or);
    } else {
      if (baseString.contains(" ")) {
        final String mapped = messageFormatter(baseString, args);
        return new PrimitiveVS<String>(mapped).restrict(pc);
      } else {
        List<GuardedValue<List<Object>>> concreteArgs =
            getConcreteValues(pc, x -> false, Concretizer::concretize, args);
        for (int i = 0; i < concreteArgs.size(); i++) {
          GuardedValue<List<Object>> guardedArgs = concreteArgs.get(i);
          List<Object> guardedArgsValues = guardedArgs.getValue();
          final String mapped =
              messageFormatter(
                  baseString, guardedArgsValues.toArray(new Object[guardedArgsValues.size()]));
          results.merge(mapped, guardedArgs.getGuard(), Guard::or);
        }
      }
    }
    return new PrimitiveVS<String>(results, true);
  }

  public static String messageFormatter(String baseString, Object... args) {
    assert (args.length != 0);
    return MessageFormat.format(baseString, args);
  }
}
