package pexplicit.utils.random;

import pexplicit.utils.exceptions.NotImplementedException;
import pexplicit.values.PValue;

import java.util.List;

/**
 * Represents a collection of static utility functions to support making a non-deterministic choice
 */
public class NondetUtil {

    /**
     * TODO
     *
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
