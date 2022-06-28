package prt.exceptions;

import prt.events.PEvent;

/**
 * Thrown by an event handler when execution of the handler should be interrupted
 * and restarted with a new event.
 */
public class RaiseEventException extends Exception {
    // XXX: We downcast to an Object since a Throwable cannot take type parameters.
    private final PEvent<Object> ev;

    public PEvent<Object> getEvent() { return ev; }

    public RaiseEventException(PEvent<Object> event) {
        ev = event;
    }
}
