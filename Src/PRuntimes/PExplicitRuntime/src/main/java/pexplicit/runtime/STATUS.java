package pexplicit.runtime;

public enum STATUS {
    INCOMPLETE,         // search still ongoing
    SCHEDULEOUT,        // schedule limit reached
    TIMEOUT,            // timeout reached
    MEMOUT,             // memout reached
    VERIFIED,           // full state space explored and no bug found
    BUG_FOUND,          // found a bug
    INTERRUPTED,        // interrupted by user
    ERROR               // unexpected error encountered
}
