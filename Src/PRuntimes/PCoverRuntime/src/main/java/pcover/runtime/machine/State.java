package pcover.runtime.machine;

import pcover.runtime.machine.eventhandlers.DeferEventHandler;
import pcover.runtime.machine.eventhandlers.EventHandler;
import pcover.runtime.machine.eventhandlers.IgnoreEventHandler;
import pcover.runtime.machine.events.PMessage;
import pcover.runtime.machine.events.StateEvents;
import pcover.utils.exceptions.NotImplementedException;
import pcover.values.PEvent;
import pcover.values.PValue;

import java.io.Serializable;
import java.util.Objects;

/**
 * Represents a machine state.
 */
public abstract class State implements Serializable {
    public final String name;
    public final String machineName;
    public final StateTemperature temperature;

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
    }

    /**
     * TODO
     *
     * @param machine
     * @param payload
     */
    public void entry(PMachine machine, PValue<?> payload) {
        throw new NotImplementedException();
    }

    /**
     * TODO
     *
     * @param machine
     */
    public void exit(PMachine machine) {
        throw new NotImplementedException();
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
     * TODO
     *
     * @return
     */
    private StateEvents getStateEvents() {
        throw new NotImplementedException();
    }

    /**
     * Register all event handlers corresponding to this state.
     *
     * @param eventHandlers Event handlers to register
     */
    public void registerHandlers(EventHandler... eventHandlers) {
        for (EventHandler handler : eventHandlers) {
            getStateEvents().eventHandlers.put(handler.event, handler);
            if (handler instanceof IgnoreEventHandler) {
                getStateEvents().ignored.add(handler.event);
            } else if (handler instanceof DeferEventHandler) {
                getStateEvents().deferred.add(handler.event);
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
        return getStateEvents().ignored.contains(event);
    }

    /**
     * Returns true if this event is deferred in this state
     *
     * @param event Event to check
     * @return true if event is deferred in this state, else false
     */
    public boolean isDeferred(PEvent event) {
        return getStateEvents().deferred.contains(event);
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
