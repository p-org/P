package psymbolic.commandline;

import lombok.Getter;
import lombok.Setter;

/**
 * Represents the configuration of the P Symbolic tool
 */
public class PSymConfiguration {

    // name of the main machine
    @Getter @Setter
    private String mainMachine = "Main";

    // max depth bound after which the search will stop automatically
    private final int maxDepthBound = 1000;

    @Getter @Setter
    // max depth bound provided by the user
    private int depthBound = maxDepthBound;

    // max input choice bound at each depth after which the search will truncate the choices
    private final int maxInputChoiceBound = 100;

    @Getter @Setter
    // max input choice bound provided by the user
    private int inputChoiceBound = maxInputChoiceBound;

    // max scheduling choice bound at each depth after which the search will truncate the scheduling choices
    private final int maxSchedChoiceBound = 100;

    @Getter @Setter
    // max input choice bound provided by the user
    private int schedChoiceBound = maxSchedChoiceBound;
}
