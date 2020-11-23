package twophasecommit;

public class RollbackException extends Exception {

    public RollbackException(String exception) {
        super(exception);
    }
}
