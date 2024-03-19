package pcover;

import org.reflections.Reflections;
import pcover.runtime.logger.Log4JConfig;
import pcover.utils.exceptions.NotImplementedException;

/**
 * PCover runtime top-level class
 */
public class PCover {

    /**
     * Main entry point for PCover runtime.
     * TODO
     */
    public static void main(String[] args) {
        // configure Log4J
        Log4JConfig.configureLog4J();

        // get reflection to fetch pcover model IR
        Reflections reflections = new Reflections("pcover.model");

        throw new NotImplementedException();
    }
}
