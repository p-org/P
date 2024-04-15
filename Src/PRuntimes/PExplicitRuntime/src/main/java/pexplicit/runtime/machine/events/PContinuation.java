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

    /**
     * Run after a machine executes this continuation
     * First, if machine is unblocked, run any pending state exit function
     * Second, if still unblocked, run any pending new state entry function
     * Third, if still unblocked, clear all continuation variables
     *
     * @param machine Machine that just executed this continuation
     */
    public void runAfter(PMachine machine) {
        // process pending state exit function first
        if (!machine.isBlocked()) {
            State blockedExitState = machine.getBlockedStateExit();
            if (blockedExitState != null) {
                assert (machine.getCurrentState() == blockedExitState);
                machine.exitCurrentState();
            }
        } else {
            // blocked on a different continuation, do nothing
            return;
        }

        // at this point, there should be no pending state exit function
        assert (machine.getBlockedStateExit() == null);

        // process any pending new state entry function
        if (!machine.isBlocked()) {
            State blockedEntryState = machine.getBlockedNewStateEntry();
            if (blockedEntryState != null) {
                machine.enterNewState(blockedEntryState, machine.getBlockedNewStateEntryPayload());
            }
        } else {
            // blocked on a different continuation, do nothing
            return;
        }

        // at this point, there should be no pending exit or new state entry functions
        assert (machine.getBlockedStateExit() == null);
        assert (machine.getBlockedNewStateEntry() == null);

        // cleanup continuation variables if unblocked completely
        if (!machine.isBlocked()) {
            for (PContinuation c : machine.getContinuationMap().values()) {
                c.getClearFun().run();
            }
        } else {
            // blocked on a different continuation, do nothing
            return;
        }
    }

}
