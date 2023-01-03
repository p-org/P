package psym.valuesummary;

import java.util.Comparator;

public class SortVS implements Comparator<PrimitiveVS> {
    public int compare(PrimitiveVS a, PrimitiveVS b) {
        int ret = 0;

//        ret = a.hashCode() - b.hashCode();
//        if (ret != 0) return ret;

        ret = a.getValues().size() - b.getValues().size();
        if (ret != 0) return ret;

        ret = a.getValues().toString().compareTo(b.getValues().toString());

        return ret;
    }
}
