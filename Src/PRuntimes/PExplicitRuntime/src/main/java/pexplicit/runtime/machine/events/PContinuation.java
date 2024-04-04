package pexplicit.runtime.machine.events;

import lombok.Getter;
import pexplicit.runtime.machine.PMachine;
import pexplicit.utils.serialize.SerializableBiFunction;
import pexplicit.utils.serialize.SerializableRunnable;
import pexplicit.values.PEvent;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

@Getter
public class PContinuation {
    private Set<String> caseEvents;
    private SerializableBiFunction<PMachine, PMessage> handleFun;
    private SerializableRunnable clearFun;

    public PContinuation(SerializableBiFunction<PMachine, PMessage> handleFun, SerializableRunnable clearFun, String ... ev) {
        this.handleFun = handleFun;
        this.clearFun = clearFun;
        this.caseEvents = new HashSet<>(Set.of(ev));
    }

    public boolean isDeferred(PEvent event) {
        return !caseEvents.contains(event.toString());
    }
}
