package pcover.runtime.machine.events;

import pcover.runtime.machine.eventhandlers.EventHandler;
import pcover.values.PEvent;

import java.io.Serializable;
import java.util.*;

/**
 * Stores details about all events corresponding to a state
 */
public class StateEvents implements Serializable {
    public final Map<PEvent, EventHandler> eventHandlers;
    public final Set<PEvent> ignored;
    public final Set<PEvent> deferred;

    /**
     * Constructor
     */
    public StateEvents() {
        this.eventHandlers = new HashMap<>();
        this.ignored = new HashSet<>();
        this.deferred = new HashSet<>();
    }

}
