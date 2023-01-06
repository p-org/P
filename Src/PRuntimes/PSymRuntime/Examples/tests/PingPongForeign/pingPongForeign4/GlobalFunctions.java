package psym.model;

import psym.runtime.values.*;

public class GlobalFunctions {

    public static void printInt(PInt i) {
        System.out.println("Printing PInt: " + i);
    }

    public static PString changeInt(PInt i) {
        PString result = new PString(Integer.toString(i.getValue()+1));
        return result;
    }

}
