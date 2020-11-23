package twophasecommit;

public class Participant {

    public int machineId;
    private boolean prepareSuccess;

    public Participant(int id, boolean success) {
        this.machineId = id;
        this.prepareSuccess = success;
    }

    public boolean prepare() {
        return prepareSuccess;
    }

    public boolean commit() {
        return true;
    }

    public boolean rollback() {
        return true;
    }
}
