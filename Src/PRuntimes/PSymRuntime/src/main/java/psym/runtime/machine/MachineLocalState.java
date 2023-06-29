package psym.runtime.machine;

import java.io.Serializable;
import java.util.List;
import java.util.Set;
import lombok.Getter;
import lombok.Setter;
import org.apache.commons.lang3.tuple.ImmutablePair;
import psym.runtime.machine.events.Event;
import psym.valuesummary.ValueSummary;

public class MachineLocalState implements Serializable {
    @Getter
    @Setter
    private List<ValueSummary> locals; // <ValueSummary>
    @Getter
    @Setter
    private Set<Event> observedEvents;
    @Getter
    @Setter
    private Set<ImmutablePair<Event, Event>> happensBeforePairs;


    public MachineLocalState() {}
}
