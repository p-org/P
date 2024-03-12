package pcover.utils.random;

import pcover.utils.exceptions.NotImplementedException;
import pcover.values.PValue;

import java.util.*;

/**
 * Represents a collection of static utility functions to support making a non-deterministic choice
 */
public class NondetUtil {

  /**
   * TODO
   * @param choices
   * @return
   */
  public static PValue<?> getNondetChoice(List<PValue<?>> choices) {
    if (choices.isEmpty())
      return null;
    if (choices.size() == 1)
      return choices.get(0);
    throw new NotImplementedException();
  }

}
