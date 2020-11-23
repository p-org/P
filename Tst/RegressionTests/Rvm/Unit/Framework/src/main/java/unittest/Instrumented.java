package unittest;

import mop.StateNameException;

public class Instrumented {

    public Instrumented() {}

    public void getState() throws StateNameException {}

    public void event1() {}
    public void event2() {}
    public void event3() {}

    public void event1Int(int a) {}
    public void event2Int(int a) {}
    public void event3Int(int a) {}
}
