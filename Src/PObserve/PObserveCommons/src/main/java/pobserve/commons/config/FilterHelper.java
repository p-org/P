package pobserve.commons.config;

import pobserve.commons.PObserveEventFilter;
import pobserve.commons.exceptions.PObserveInternalException;

public class FilterHelper implements InstanceHelper {
    /*
     * Checks if the class is the required filter class
     * @param c the class that needs to be checked
     * @param filterName exact class name of the filter
     * @return true if the current class is the filter class else false
     * */
    @Override
    public boolean isInstanceOf(Class c, String filterName) {
        if (PObserveEventFilter.class.isAssignableFrom(c) && PObserveEventFilter.class != c) {
            return c.getSimpleName().equals(filterName);
        }
        return false;
    }

    /*
     * Returns new instance of filter from the input jar
     * @param filterClass the class object of pobserve event filter
     * @return new instance of the filter class
     * */
    @Override
    public Object getInstance(Class filterClass) {
        try {
            return filterClass.getConstructor().newInstance();
        } catch (Exception e) {
            throw new PObserveInternalException("Exception occurred while creating PObserveEventFilter instance", e);
        }
    }
}
