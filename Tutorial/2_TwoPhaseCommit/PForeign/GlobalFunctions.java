package psym.model;

import psym.runtime.values.*;
import java.util.ArrayList;
import java.util.List;
import java.util.HashMap;
import java.util.Map;
import java.util.Random;

public class GlobalFunctions {
    public static PNamedTuple ChooseRandomTransaction(PInt uniqueId) {
        List<String> fields = new ArrayList<>();
        fields.add("key");
        fields.add("val");
        fields.add("transId");

        Map<String, PValue<?>> values = new HashMap<>();
        Random rand = new Random();
        values.put("key", new PString(String.format("%d", rand.nextInt(10))));
        values.put("val", new PInt(rand.nextInt(10)));
        values.put("transId", new PInt(uniqueId));

        return new PNamedTuple(fields, values);
    }
}
