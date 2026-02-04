package pobserve.junit;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;
import java.util.function.Supplier;

import pobserve.commons.Parser;
import pobserve.runtime.events.PEvent;

/**
 * Annotation for JUnit Spec Configurations, used in PObserveLog4JBaseTest
 * Usage: Initialize before each test class that extends PObserveLog4JBaseTest
 */
@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.TYPE) // Targeting class-level annotation
public @interface PObserveJUnitSpecConfig {
    Class<? extends Parser<? extends PEvent<?>>> parser();

    Class<? extends Supplier<?>>[] monitors();

    String appenderName() default "";
}
