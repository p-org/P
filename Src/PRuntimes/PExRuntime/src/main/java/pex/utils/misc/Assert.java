package pex.utils.misc;

import lombok.Getter;
import pex.utils.exceptions.BugFoundException;
import pex.utils.exceptions.DeadlockException;
import pex.utils.exceptions.LivenessException;
import pex.values.PString;

public class Assert {
    @Getter
    private static String failureType = "";
    @Getter
    private static String failureMsg = "";

    public static void fromModel(boolean p, String msg) {
        if (!p) {
            failureType = "prop";
            failureMsg = "Property violated: " + msg;
            throw new BugFoundException(failureMsg);
        }
    }

    public static void fromModel(boolean p, PString msg) {
        fromModel(p, msg.getValue());
    }

    public static void deadlock(String msg) {
        failureType = "deadlock";
        failureMsg = "Property violated: " + msg;
        throw new DeadlockException(failureMsg);
    }

    public static void liveness(String msg) {
        failureType = "liveness";
        failureMsg = "Property violated: " + msg;
        throw new LivenessException(failureMsg);
    }

    public static void cycle(String msg) {
        failureType = "cycle";
        failureMsg = "Property violated: " + msg;
        throw new LivenessException(failureMsg);
    }
}