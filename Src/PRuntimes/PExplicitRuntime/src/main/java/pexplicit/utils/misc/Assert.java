package pexplicit.utils.misc;

import lombok.Getter;
import pexplicit.utils.exceptions.BugFoundException;
import pexplicit.utils.exceptions.LivenessException;
import pexplicit.values.PString;

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

    public static void liveness(boolean p, String msg) {
        if (!p) {
            failureType = "liveness";
            failureMsg = "Property violated: " + msg;
            throw new LivenessException(failureMsg);
        }
    }

    public static void cycle(boolean p, String msg) {
        if (!p) {
            failureType = "cycle";
            failureMsg = "Property violated: " + msg;
            throw new LivenessException(failureMsg);
        }
    }
}