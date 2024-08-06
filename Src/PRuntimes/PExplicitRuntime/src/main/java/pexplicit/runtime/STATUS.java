package pexplicit.runtime;

public enum STATUS {
    INCOMPLETE("incomplete"),                           // search still ongoing
    SCHEDULEOUT("scheduleout"),                         // schedule limit reached
    TIMEOUT("timeout"),                                 // timeout reached
    MEMOUT("memout"),                                   // memout reached
    VERIFIED("verified"),                               // full state space explored and no bug found
    VERIFIED_UPTO_MAX_STEPS("verified"),                // full state space explored and no bug found upto max steps
    BUG_FOUND("cex"),                                   // found a bug
    INTERRUPTED("interrupted"),                         // interrupted by user
    ERROR("error");                                     // unexpected error encountered

    private String name;

    /**
     * Constructor
     *
     * @param n Name of the enum
     */
    STATUS(String n) {
        this.name = n;
    }

    @Override
    public String toString() {
        return this.name;
    }
}
