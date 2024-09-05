package pex.model;

import pex.values.*;
import java.util.ArrayList;
import java.util.List;
import java.util.HashMap;
import java.util.Map;
import java.util.Random;

public class PExForeignCode {
/* Not allowed to use data non-determinism in PEx foreign code. So we cannot do the following:
    public static PNamedTuple ChooseRandomTransaction(PInt uniqueId) {
        Map<String, PValue<?>> values = new HashMap<>();
        Random rand = new Random();
        values.put("key", new PString(String.format("%d", rand.nextInt(10))));
        values.put("val", new PInt(rand.nextInt(10)));
        values.put("transId", new PInt(uniqueId));

        return new PNamedTuple(values);
    }
*/
}
