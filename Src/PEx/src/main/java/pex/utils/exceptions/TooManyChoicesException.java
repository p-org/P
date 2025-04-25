package pex.utils.exceptions;

import lombok.Getter;
import pex.runtime.PExGlobal;

public class TooManyChoicesException extends BugFoundException {
    @Getter
    String loc = "";

    /**
     * Constructs a new TooManyChoicesException for choose(.) with too choices in a single call.
     *
     * @param loc        location of the choice
     * @param numChoices number of choices in given choose(.) call from this location
     */
    public TooManyChoicesException(String loc, int numChoices) {
        super(String.format("%s: choose expects a parameter with at most %d choices, got %d choices instead.",
                loc, PExGlobal.getConfig().getMaxChoicesPerStmtPerCall(), numChoices));
        this.loc = loc;
    }

    /**
     * Constructs a new TooManyChoicesException for choose(.) with too choices across calls.
     *
     * @param loc        location of the choice
     * @param numChoices number of choices in total from this location
     * @param numCalls   number of choose(.) calls from this location
     */
    public TooManyChoicesException(String loc, int numChoices, int numCalls) {
        super(String.format("""
                        %s: too many choices generated from this statement - total %d choices after %d choose calls.
                        Reduce the total number of choices generated here to at most %d, by reducing the number of times this choose statement is called.""",
                loc, numChoices, numCalls, PExGlobal.getConfig().getMaxChoicesPerStmtPerSchedule()));
        this.loc = loc;
    }
}
