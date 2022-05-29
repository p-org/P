package pcontainment.runtime;


import lombok.Getter;
import pcontainment.runtime.machine.Machine;
import pcontainment.runtime.machine.MachineIdentifier;

import java.util.*;

/**
 * Represents a message in the sender buffer of a state machine
 */
public class Message {

    // the target machine id to which the message is being sent
    @Getter
    private final MachineIdentifier targetId;
    // the event id sent to the target machine
    @Getter
    private final Event event;
    // the flattened payloads associated with the event
    @Getter
    public final Payloads payloads;

    public Message(Event event, MachineIdentifier targetId, Payloads payloads) {
        this.targetId = targetId;
        this.event = event;
        if (payloads == null)
            this.payloads = new Payloads();
        else
          this.payloads = payloads;
    }

    @Override
    public String toString() {
        return "Send " + event.toString() + " to " + targetId;
    }

}
