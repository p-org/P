package psym.runtime.logger;

import lombok.Getter;
import org.apache.logging.log4j.Level;
import org.apache.logging.log4j.core.LoggerContext;
import org.apache.logging.log4j.core.config.Configurator;
import org.apache.logging.log4j.core.config.builder.api.ConfigurationBuilder;
import org.apache.logging.log4j.core.config.builder.api.ConfigurationBuilderFactory;
import org.apache.logging.log4j.core.config.builder.impl.BuiltConfiguration;
import org.apache.logging.log4j.core.layout.PatternLayout;
import org.reflections.Reflections;

public class Log4JConfig {
  private static final String pattern = "%msg%n";
  @Getter private static LoggerContext context = null;
  @Getter private static PatternLayout patternLayout = null;

  public static void configureLog4J() {
    ConfigurationBuilder<BuiltConfiguration> builder =
        ConfigurationBuilderFactory.newConfigurationBuilder();

    // configure a console appender
    builder.add(
        builder
            .newAppender("stdout", "Console")
            .add(
                builder
                    .newLayout(PatternLayout.class.getSimpleName())
                    .addAttribute("pattern", pattern)));

    // configure the root logger
    builder.add(builder.newRootLogger(Level.INFO).add(builder.newAppenderRef("stdout")));

    // apply the configuration
    context = Configurator.initialize(builder.build());

    // set pattern layout
    patternLayout = PatternLayout.newBuilder().withPattern(pattern).build();

    // set level for reflections class
    Configurator.setLevel(Reflections.class, Level.ERROR);
  }
}
