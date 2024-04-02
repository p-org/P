package pexplicit.runtime.machine;

import pexplicit.runtime.machine.eventhandlers.DeferEventHandler;
import pexplicit.runtime.machine.eventhandlers.EventHandler;
import pexplicit.runtime.machine.eventhandlers.IgnoreEventHandler;
import pexplicit.runtime.machine.events.PMessage;
import pexplicit.runtime.machine.events.StateEvents;
import pexplicit.utils.exceptions.NotImplementedException;
import pexplicit.values.PEvent;
import pexplicit.values.PValue;

import java.io.Serializable;
import java.util.Objects;

/**
 * Represents a machine state.
 */
public abstract class State implements Serializable {
    public final String name;
    public final String machineName;
    public final StateTemperature temperature;
    public final StateEvents stateEvents;

    /**
     * Constructor
     *
     * @param name        Name of the state
     * @param machineName Machine name
     * @param temperature State temperature
     */
    public State(
            String name,
            String machineName,
            StateTemperature temperature) {
        this.name = name;
        this.machineName = machineName;
        this.temperature = temperature;
        this.stateEvents = new StateEvents();
    }

    /**
     * Default entry function for a state
     *
     * @param machine Machine entering the state
     * @param payload Entry function payload
     */
    public void entry(PMachine machine, PValue<?> payload) {
    }

    /**
     * Default exit function for a state
     *
     * @param machine Machine exiting the state
     */
    public void exit(PMachine machine) {
    }

    /**
     * Get a unique key corresponding to this machine state
     *
     * @return
     */
    private String getStateKey() {
        return String.format("%s_%s", name, machineName);
    }

    /**
     * Register all event handlers corresponding to this state.
     *
     * @param eventHandlers Event handlers to register
     */
    public void registerHandlers(EventHandler... eventHandlers) {
        for (EventHandler handler : eventHandlers) {
            stateEvents.eventHandlers.put(handler.event, handler);
            if (handler instanceof IgnoreEventHandler) {
                stateEvents.ignored.add(handler.event);
            } else if (handler instanceof DeferEventHandler) {
                stateEvents.deferred.add(handler.event);
            }
        }
    }

    /**
     * Returns true if this event is ignored in this state
     *
     * @param event Event to check
     * @return true if event is ignored in this state, else false
     */
    public boolean isIgnored(PEvent event) {
        return stateEvents.ignored.contains(event);
    }

    /**
     * Returns true if this event is deferred in this state
     *
     * @param event Event to check
     * @return true if event is deferred in this state, else false
     */
    public boolean isDeferred(PEvent event) {
        return stateEvents.deferred.contains(event);
    }

    /**
     * TODO
     *
     * @param message
     * @param machine
     */
    public void handleEvent(PMessage message, PMachine machine) {
        throw new NotImplementedException();
    }

    /**
     * Returns true if this state is a hot state
     *
     * @return true if this state is hot, else false
     */
    public boolean isHotState() {
        return temperature == StateTemperature.Hot;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof State)) {
            return false;
        }
        return this.name.equals(((State) obj).name)
                && this.machineName.equals(((State) obj).machineName)
                && this.temperature.equals(((State) obj).temperature);
    }

    @Override
    public int hashCode() {
        return Objects.hash(name, machineName, temperature);
    }

    @Override
    public String toString() {
        return name;
    }
}
