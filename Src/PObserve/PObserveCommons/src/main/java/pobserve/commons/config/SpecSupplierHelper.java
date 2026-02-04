package pobserve.commons.config;

import pobserve.commons.exceptions.PObserveInternalException;
import java.util.function.Supplier;

public class SpecSupplierHelper implements InstanceHelper {
    /*
     * Checks if the class is the required specification class
     * @param c the class that needs to be checked
     * @param specName exact class name of the specification
     * @return true if the current class is the specification class else false
     * */
    @Override
    public boolean isInstanceOf(Class c, String specName) {
        return Supplier.class.isAssignableFrom(c) && Supplier.class != c && c.getName().contains(specName);
    }

    /*
     * Returns instance of specification supplier from the input jar
     * @param monitorClass the class object of the specification
     * @return a new instance of specification supplier
     * */
    @Override
    public Object getInstance(Class monitorClass) {
        Class<? extends Supplier> consumerSupplierClass = monitorClass.asSubclass(Supplier.class);
        try {
            return consumerSupplierClass.getConstructor().newInstance();
        } catch (Exception e) {
            throw new PObserveInternalException("Exception occurred in creating Monitor instance", e);
        }
    }
}
