package pexplicit;

import org.reflections.Reflections;
import pexplicit.runtime.logger.Log4JConfig;
import pexplicit.utils.exceptions.NotImplementedException;

/**
 * PExplicit runtime top-level class
 */
public class PExplicit {

    /**
     * Main entry point for PExplicit runtime.
     * TODO
     */
    public static void main(String[] args) {
        // configure Log4J
        Log4JConfig.configureLog4J();

        // get reflection to fetch pexplicit model IR
        Reflections reflections = new Reflections("pexplicit.model");

        throw new NotImplementedException();
    }
}
