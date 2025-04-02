package pex.runtime.machine.events;

import pex.runtime.machine.eventhandlers.EventHandler;
import pex.values.Event;

import java.io.Serializable;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;

/**
 * Stores details about all events corresponding to a state
 */
public class StateEvents implements Serializable {
    public final Map<Event, EventHandler> eventHandlers;
    public final Set<Event> ignored;
    public final Set<Event> deferred;

    /**
     * Constructor
     */
    public StateEvents() {
        this.eventHandlers = new HashMap<>();
        this.ignored = new HashSet<>();
        this.deferred = new HashSet<>();
    }

}
