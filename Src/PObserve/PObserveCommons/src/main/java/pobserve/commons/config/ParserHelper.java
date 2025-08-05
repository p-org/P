package pobserve.commons.config;

import pobserve.commons.Parser;
import pobserve.commons.exceptions.PObserveInternalException;

public class ParserHelper implements InstanceHelper {
    /*
     * Checks if the class is the required parser class
     * @param c the class that needs to be checked
     * @param parserName exact class name of the parser
     * @return true if the current class is the parser class else false
     * */
    @Override
    public boolean isInstanceOf(Class c, String parserName) {
        if (Parser.class.isAssignableFrom(c) && Parser.class != c) {
            return parserName == null || c.getSimpleName().equals(parserName);
        }
        return false;
    }

    /*
     * Returns new instance of parser from the input jar
     * @param parserClass the class object of parser
     * @return new instance of parser
     * */
    @Override
    public Object getInstance(Class parserClass) {
        try {
            return parserClass.getConstructor().newInstance();
        } catch (Exception e) {
            throw new PObserveInternalException("Exception occurred while creating Parser instance", e);
        }
    }
}
