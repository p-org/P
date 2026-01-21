package pobserve.junit.utils;

import java.lang.reflect.InvocationTargetException;
import java.util.ArrayList;
import java.util.List;
import java.util.function.Supplier;

import pobserve.commons.Parser;
import pobserve.runtime.events.PEvent;

public class ParserAndMonitorProvider {
    /**
     * Create instances of monitorSuppliers from the class
     *
     * @param supplierClasses suplier classes from specConfig annotation
     * @return
     */
    public static List<Supplier<?>> getMonitorSuppliers(Class<? extends Supplier<?>>[] supplierClasses) {
        List<Supplier<?>> monitorSuppliers = new ArrayList<>();
        for (Class<? extends Supplier<?>> monitorSupplier : supplierClasses) {
            try {
                monitorSuppliers.add(monitorSupplier.getDeclaredConstructor().newInstance());
            } catch (Exception e) {
                throw new RuntimeException("Failed to instantiate monitors", e);
            }
        }
        return monitorSuppliers;
    }

    /**
     *  Initiating new instance of parser
     *
     * @param parserClass parser class from specConfig annotation
     */
    public static Parser getParser(Class<? extends Parser<? extends PEvent<?>>> parserClass) {
        Parser parser;
        try {
            parser = parserClass.getDeclaredConstructor().newInstance();
        } catch (InstantiationException | IllegalAccessException |
                 NoSuchMethodException | InvocationTargetException e) {
            throw new RuntimeException("Can not create an instance of the parser", e);
        }
        return parser;
    }
}
