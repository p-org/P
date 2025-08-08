package pobserve;


import java.time.Duration;

public class PObserveTest {
  private static final Duration DEFAULT_MAX_OUT_OF_ORDERLINESS = Duration.ZERO;

  /*@Test
  public void orderTest() throws Exception {
      List<PObserveEvent> inputStream = List.of(
              new PObserveEvent("key1", 4L, new OrderMonitor.DefaultEvent(4L)),
              new PObserveEvent("key1", 3L, new OrderMonitor.DefaultEvent(3L)),
              new PObserveEvent("key1", 2L, new OrderMonitor.DefaultEvent(2L)),
              new PObserveEvent("key1", 1L, new OrderMonitor.DefaultEvent(1L)));
      Source source = new Source(inputStream, DEFAULT_MAX_OUT_OF_ORDERLINESS);
      List<Sink> sinks = List.of(new Sink(new OrderMonitorSupplier()));
      Sequencer sequencer = new Sequencer(source, sinks);
      sequencer.execute();
  }*/

}
