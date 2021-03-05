package com.runtimeverification.rvmonitor.java.rt.annotation;

import java.lang.annotation.Target;
import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Repeatable;

@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.METHOD)
@Repeatable(MonitorsOn.class)
public @interface MonitorOn {
    String value() default "";
}


