# PUnit Unit Test Library

PUnit is a library that extends JUnit test suites with P specifications.

## Dependencies

The P Java runtime (PRT) is required on your classpath.  You likely already
have it installed by virtue of executing P monitors extracted to Java; but, to
install it from this repo into your local Maven repository,

```
$ cd Src/PRuntimes/PJavaRuntime
$ mvn install
```

All other dependencies will be pulled from remote repositories.

## Overview and Tutorial

Imagine I have an implementation of an algebraic ring (I can add and multiply
to it), just as a silly example.  If I have a unit test that looks like this:

```java
    @Test
    public void testSingleRingMul() {
        Ring r = new Ring();
        r.Add(32);
        r.Add(10);
        assertEquals(r.getVal(), 42);
    }
```

Let’s say the Ring emits the following log lines over its log4j logger::

```
15:17:48.116 [main] INFO  Ring - : ADD:32,32
15:17:48.118 [main] INFO  Ring - : ADD:10,42
```

Critically: note that these log lines are coming from the `Ring` logger.
Now, imagine I wrote a specification for the Ring, just confirming that we can
add and multiply correctly (perhaps with a BigInteger to confirm we handle
unbounded operations correctly):

```java
    @Test
    @DisplayName("Test no overflow, manually")
    public void testSpecOverflow() {
        Monitor spec = new RingSpec();
        spec.ready();

        spec.accept(
                new PEvents.addEvent(
                        new PTypes.PTuple_i_total(32, 32)));
        spec.accept(
                new PEvents.addEvent(
                        new PTypes.PTuple_i_total(10, 42)));
    }
```

We could see that this spec receives two events and does something with them
(lines 3 and 5 are emitted by the event handlers; everything else is part of
the internal monitor implementation).

```
15:19:19.038 [main] INFO  RingSpec - STATE_TRANSITIONING: state="prt.State[INIT]"
15:19:19.043 [main] INFO  RingSpec - EVENT_PROCESSING: event="addEvent[PTuple_i_total[i=32, total=32]]"
15:19:19.066 [main] INFO  RingSpec - : 0 + 32 = 32
15:19:19.066 [main] INFO  RingSpec - EVENT_PROCESSING: event="addEvent[PTuple_i_total[i=10, total=42]]"
15:19:19.067 [main] INFO  RingSpec - : 32 + 10 = 42
```

Of course, the thing we _want_ is to run the first test, and have the output of
the second test.  So, let me change the `@Test` annotation to a `@PSpecTest`:

```java
    @PSpecTest(impl = Ring.class, parser = RingEventParser.Supplier.class, spec = RingSpec.Supplier.class)
    @DisplayName("Can multiply to a Ring specification, by way of driving the implementation")
    public void testSingleRingMul() {
        Ring r = new Ring();
        r.Add(32);
        r.Add(10);
    }
```

This annotation says:

1) For all instances of the `Ring` class in this test, intercept all messages
written to the Log4j appender for that class;
2) For each log line, parse it into a PEvent according to the parser created by
the given supplier;
3) Hand the parsed event to a Monitor created by the given spec supplier.

With this new annotation, we can drive the original implementation but observe exactly
the same output as if we'd driven the spec manually:

```
15:25:16.734 [main] INFO  RingSpec - STATE_TRANSITIONING: state="prt.State[INIT]"
15:25:16.771 [main] INFO  RingSpec - EVENT_PROCESSING: event="addEvent[PTuple_i_total[i=32, total=32]]"
15:25:16.793 [main] INFO  RingSpec - : 0 + 32 = 32
15:25:16.794 [main] INFO  RingSpec - EVENT_PROCESSING: event="addEvent[PTuple_i_total[i=10, total=42]]"
15:25:16.795 [main] INFO  RingSpec - : 32 + 10 = 42
```

Further examples with this implementation and specification can be found in the `test/java/sample` directory.
