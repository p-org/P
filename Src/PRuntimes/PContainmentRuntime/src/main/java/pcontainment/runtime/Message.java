package pcontainment.runtime;


import jdk.internal.net.http.common.Pair;
import lombok.Getter;

import java.util.*;

/**
 * Represents a message in the sender buffer of a state machine
 */
public class Message {

    // the target machine id to which the message is being sent
    @Getter
    private final int target;
    // the event id sent to the target machine
    @Getter
    private final int event;
    // the flattened payloads associated with the event
    @Getter
    public final Map<String, Object> payloads;

    public Message(int target, int event, Map<String, Object> payloads) {
        this.target = target;
        this.event = event;
        if (payloads == null)
            this.payloads = new HashMap<>();
        else
          this.payloads = payloads;
    }

}
