package pcontainment.runtime;

import lombok.Getter;
import lombok.Setter;

import java.util.ArrayList;
import java.util.List;

public class Observation {
    public final Message receive;
    public final List<Message> sends;
    @Getter @Setter
    private boolean partial = true;
    @Getter @Setter
    private boolean started = false;
    @Getter @Setter
    private int sendIdx = 0;

    public Observation(Message receive) {
        this.receive = receive;
        this.sends = new ArrayList<>();
    }

    public void addSend(Message send) {
        sends.add(send);
    }
}