package psymbolic.runtime;

import java.util.Objects;

public class Event {

    public static Event Init = new Event("Init");
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


