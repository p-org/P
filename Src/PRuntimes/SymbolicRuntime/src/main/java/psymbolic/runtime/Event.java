package psymbolic.runtime;

import lombok.AllArgsConstructor;

import java.util.Objects;

/**
 * Represents a P event (Event Name)
 */
@AllArgsConstructor
public class Event {
    // Special event send to a machine on creation
    public static Event createMachine = new Event("createMachine");
    // Name of the Event
    final String name;

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


