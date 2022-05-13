package psymbolic.runtime;

import java.io.Serializable;
import java.util.Objects;

/**
 * Represents a P event (Event Name)
 */
public class Event implements Serializable {
    // Special event send to a machine on creation
    public static Event createMachine = new Event("createMachine");
    // Special halt event
    public static Event haltMachine = new Event("_halt");
    // Name of the Event
    final String name;

    public Event(String name) {
        this.name = name;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof Event)) {
            return false;
        }
        return this.name.equals(((Event) obj).name);
    }

    @Override
    public int hashCode() {
        return Objects.hash(name);
    }

    @Override
    public String toString() {
        return name;
    }
}


