package psym.model;

import psym.runtime.values.*;

public class GlobalFunctions {

    public static void printInt(PInt i) {
        System.out.println("Printing PInt: " + i);
    }

    public static PInt changeInt(PInt i) {
        PInt result = new PInt(i.getValue()+1);
        return result;
    }

}
