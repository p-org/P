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

    public static CustomInt convertToCustomInt(PInt val) {
        return new CustomInt(val.getValue());
    }

    public static PInt convertFromCustomInt(CustomInt val) {
        return new PInt(val.getValue());
    }

    public static CustomInt changeCustomInt(CustomInt val) {
        return new CustomInt(val.getValue()+1);
    }

}
