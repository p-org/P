package prt;

/**
 * Thrown when the prt.Monitor tries to pass a state transition payload of the wrong type to
 * the entry handler for a new state.
 */
public class GotoPayloadClassException extends RuntimeException {
    private Class<?> payloadClazz;
    private State s;

    public <P> GotoPayloadClassException(P payload, State s) {
        this.payloadClazz = payload.getClass();
        this.s = s;
    }

    @Override
    public String getMessage() {
        return String.format("Got invalid payload of type %s for state %s's onEntry handler.",
                payloadClazz.getName(), s.getKey());
    }
}
