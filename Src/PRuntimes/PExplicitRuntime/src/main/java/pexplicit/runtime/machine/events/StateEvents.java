package pexplicit.runtime.machine.events;

import pexplicit.runtime.machine.eventhandlers.EventHandler;
import pexplicit.values.PEvent;

import java.io.Serializable;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;

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
