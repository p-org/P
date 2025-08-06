package pobserve.junit.log4j;

import org.apache.logging.log4j.core.Layout;
import org.apache.logging.log4j.core.LogEvent;
import org.apache.logging.log4j.core.appender.AbstractAppender;
import org.apache.logging.log4j.core.config.plugins.Plugin;
import org.apache.logging.log4j.core.config.plugins.PluginAttribute;
import org.apache.logging.log4j.core.config.plugins.PluginElement;
import org.apache.logging.log4j.core.config.plugins.PluginFactory;
import org.junit.jupiter.api.TestInfo;

import pobserve.commons.Parser;
import pobserve.junit.PObserveLogAppender;
import pobserve.runtime.events.PEvent;

import java.io.Serializable;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.util.function.Consumer;
import java.util.function.Supplier;

/**
 * PObserveLog4JAppender is a custom appender that can be used to parse log4J log lines
 * and pass them into specified parser, then pass parsed events to event sequencer as a stream
 */
@Plugin(name = "PObserveLog4JAppender", category = "Core", printObject = true)
public final class PObserveLog4JAppender extends AbstractAppender {

    private final PObserveLogAppender pobserveLogAppender;

    public PObserveLog4JAppender(String name,
                                 Layout<? extends Serializable> layout,
                                 Parser<? extends PEvent<?>> parser,
                                 List<Supplier<?>> monitorSuppliers) {
        super(name, null, layout, true);
        this.pobserveLogAppender = new PObserveLogAppender(parser, monitorSuppliers);
    }

    /** Factory method to create the appender
     *
     * @param name the name of the appender
     * @param layout the pattern for the formatted messages
     * @param parserClass the name of the PObserve parser class
     * @param supplierClasses The list of monitor supplier classes, separated by commas
     *
     * @return the appender
     * @throws Exception if we encounter issues configuring this appender instance
     */
    @PluginFactory
    public static PObserveLog4JAppender createAppender(
            @PluginAttribute("name") String name,
            @PluginElement("pattern") Layout<? extends Serializable> layout,
            @PluginAttribute("parserClass") String parserClass,
            @PluginAttribute("supplierClasses") String supplierClasses) throws Exception {

        if (name == null) {
            LOGGER.error("No name provided for the log4J appender.");
            return null;
        }

        var parser = getParser(parserClass);
        var monitorSuppliers = getMonitorSuppliers(supplierClasses);
        return new PObserveLog4JAppender(name, layout, parser, monitorSuppliers);
    }

    public static Object createInstance(String className) throws Exception {
        Class<?> clazz = Class.forName(className);
        return clazz.getDeclaredConstructor().newInstance();
    }

    public static Parser<? extends PEvent<?>> getParser(String className) throws Exception {
        Object object = createInstance(className);
        Parser<? extends PEvent<?>> parser = (object instanceof Parser) ? (Parser) object : null;
        return parser;
    }

    public static List<Supplier<?>> getMonitorSuppliers(String classNames) throws Exception {
        String[] classNameList = classNames.split(",");
        List<Supplier<?>> monitorSuppliers = new ArrayList<>();

        for (String className : classNameList) {
            Object object = createInstance(className);
            monitorSuppliers.add((Supplier<Consumer<Object>>) object);
        }
        return monitorSuppliers;
    }

    public PObserveLogAppender getPObserveLogAppender() {
        return pobserveLogAppender;
    }

    @Override
    public void append(LogEvent event) {
        if (event == null) {
            return;
        }
        String line = new String(getLayout().toByteArray(event), StandardCharsets.UTF_8);
        this.pobserveLogAppender.append(line);
    }

    public void close(TestInfo testInfo) {
        this.pobserveLogAppender.close(testInfo);
        super.stop();
    }

    public void close() {
        this.pobserveLogAppender.close();
        super.stop();
    }
}
