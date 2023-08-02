package psym.runtime.machine.events;

import java.io.Serializable;
import java.util.*;
import psym.runtime.machine.eventhandlers.EventHandler;

public class StateEvents implements Serializable {
    public final Map<Event, EventHandler> eventHandlers;
    public final Set<Event> ignored;
    public final Set<Event> deferred;

    public StateEvents() {
        this.eventHandlers = new HashMap<>();
        this.ignored = new HashSet<>();
        this.deferred = new HashSet<>();
    }

}
