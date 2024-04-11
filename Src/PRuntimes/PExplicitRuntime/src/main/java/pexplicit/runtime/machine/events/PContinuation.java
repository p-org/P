package pexplicit.runtime.machine.events;

import lombok.Getter;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.State;
import pexplicit.utils.serialize.SerializableBiFunction;
import pexplicit.utils.serialize.SerializableRunnable;
import pexplicit.values.PEvent;

import java.util.HashSet;
import java.util.Set;

@Getter
public class PContinuation {
    private final Set<String> caseEvents;
    private final SerializableBiFunction<PMachine, PMessage> handleFun;
    private final SerializableRunnable clearFun;

    public PContinuation(SerializableBiFunction<PMachine, PMessage> handleFun, SerializableRunnable clearFun, String... ev) {
        this.handleFun = handleFun;
        this.clearFun = clearFun;
        this.caseEvents = new HashSet<>(Set.of(ev));
    }

    public boolean isDeferred(PEvent event) {
        return !event.isHaltMachineEvent() && !caseEvents.contains(event.toString());
    }

    public void runAfter(PMachine machine) {
        // process any pending exit state
        if (!machine.isBlocked()) {
            State blockedExitState = machine.getBlockedExitState();
            if (blockedExitState != null) {
                assert (machine.getCurrentState() == blockedExitState);
                machine.exitCurrentState();
            }
        }

        // process any pending entry state
        if (!machine.isBlocked()) {
            State blockedEntryState = machine.getBlockedEntryState();
            if (blockedEntryState != null) {
                machine.enterNewState(blockedEntryState, machine.getBlockedEntryPayload());
            }
        }

        // cleanup continuations if unblocked completely
        if (!machine.isBlocked()) {
            for (PContinuation c : machine.getContinuationMap().values()) {
                c.getClearFun().run();
            }
        }
    }

}
