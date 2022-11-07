package psymbolic.runtime.scheduler.choiceorchestration;

import lombok.Getter;
import lombok.Setter;
import psymbolic.runtime.Event;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.State;

import java.io.Serializable;
import java.util.Objects;

public class ChoiceFeature implements Serializable {
    private final Machine source;
    private final Machine target;
    private final State state;
    private final Event event;
    @Getter @Setter
    private int id;

    @Getter @Setter
    private ChoiceReward reward;

    public ChoiceFeature(Machine src, Machine tgt, State s, Event e) {
        source = src;
        target = tgt;
        state = s;
        event = e;
        id = -1;
        reward = null;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) {
            return true;
        } else if (!(obj instanceof ChoiceFeature)) {
            return false;
        }
        ChoiceFeature rhs = (ChoiceFeature) obj;
        if (this.id == -1 || rhs.id == -1) {
            return  this.source.equals(rhs.source) &&
                    this.target.equals(rhs.target) &&
                    this.state.equals(rhs.state) &&
                    this.event.equals(rhs.event);
        } else if (this.id == rhs.id) {
            assert(this.source.equals(rhs.source));
            assert(this.target.equals(rhs.target));
            assert(this.state.equals(rhs.state));
            assert(this.event.equals(rhs.event));
            return true;
        } else {
            return false;
        }
    }

    @Override
    public int hashCode() {
        return Objects.hash(source, target, state, event);
    }

    @Override
    public String toString() {
        return String.format("%s [%s] --[%s]--> %s", source, state, event, target);
    }
}
