package pcontainment.runtime.machine.eventhandlers;

import lombok.Getter;
import pcontainment.runtime.Event;
import pcontainment.runtime.Message;
import pcontainment.runtime.Payloads;
import pcontainment.runtime.machine.State;
import java.util.Map;

/**
 * Represent the outcome of executing an event handler 
 * Either a normal return, or goto, or raise.
 */
public class EventHandlerReturnReason {

    private EventHandlerReturnReason() {};

    public static class Raise extends EventHandlerReturnReason {
        @Getter
        private final Message message;
        public Raise(Message m) {
            message = m;
        }
        public Raise(Event e, Payloads payloads) {
            message = new Message(e, null, payloads);
        }
    }

    public static class Goto extends EventHandlerReturnReason {
        @Getter
        private final Payloads payloads;
        @Getter
        private final State goTo;
        public Goto(State s) {
            goTo = s;
            payloads = new Payloads();
        }
        public Goto(State s, Message m) {
            goTo = s;
            payloads = m.getPayloads();
        }
    }

    public static class NormalReturn extends EventHandlerReturnReason {
        public NormalReturn() {}
    }

}
