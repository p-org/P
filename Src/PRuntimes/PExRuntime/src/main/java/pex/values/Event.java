package pex.values;

/**
 * Represents a P event
 */
public class Event extends PValue<Event> {
    /**
     * Special event send to a machine on creation
     */
    public static final Event createMachine = new Event("createMachine");

    /**
     * Special halt event
     */
    public static final Event haltEvent = new Event("_halt");

    /**
     * Name of the event
     */
    final String name;

    /**
     * Constructor.
     *
     * @param name Event name
     */
    public Event(String name) {
        this.name = name;
        initialize();
    }

    /**
     * Constructor.
     *
     * @param event Event
     */
    public Event(Event event) {
        this.name = event.name;
        initialize();
    }

    public boolean isCreateMachineEvent() {
        return this.equals(createMachine);
    }

    public boolean isHaltMachineEvent() {
        return this.equals(haltEvent);
    }

    @Override
    public Event clone() {
        return new Event(name);
    }

    @Override
    protected String _asString() {
        return name;
    }

    @Override
    public Event getDefault() {
        return null;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof Event)) {
            return false;
        }
        return this.name.equals(((Event) obj).name);
    }
}
