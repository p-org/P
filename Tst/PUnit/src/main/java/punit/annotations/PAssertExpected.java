package punit.annotations;

import org.junit.jupiter.api.Tag;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * Annotates that a PTest should result in a PAssertionFailureException thrown (i.e.
 * that an P-language assert failed).  Throws if a test annotated with this does not!
 */
@Target({ ElementType.METHOD })
@Retention(RetentionPolicy.RUNTIME)
@Tag("PSpecTest")
public @interface PAssertExpected {

}
