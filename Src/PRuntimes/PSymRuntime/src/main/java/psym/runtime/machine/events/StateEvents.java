package psym.runtime.machine.events;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import psym.runtime.machine.eventhandlers.EventHandler;

public class StateEvents implements Serializable {
    public final Map<Event, EventHandler> eventHandlers;
    public final List<Event> ignored;

    public StateEvents() {
        this.eventHandlers = new HashMap<>();
        this.ignored = new ArrayList<>();
    }

}
