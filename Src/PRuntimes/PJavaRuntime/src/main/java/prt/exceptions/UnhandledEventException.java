package prt.exceptions;

import prt.PState;
import prt.PEvent;

/**
 * Thrown when a given prt.State has no handler for a given P Event.
 */
public class UnhandledEventException extends RuntimeException {
    private PState state;
    private Class<? extends PEvent> clazz;

    /**
     * Instantiates a new prt.exceptions.UnhandledEventException.
     *
     * @param s the state missing some event.
     * @param c the subclass of Event.Payload without a handler.
     */
    public UnhandledEventException(PState s, Class<? extends PEvent> c) {
        state = s;
        clazz = c;
    }

    @Override
    public String toString() {
        return String.format("prt.State %s has no handler for class %s", this.state, clazz.getName());
    }
}