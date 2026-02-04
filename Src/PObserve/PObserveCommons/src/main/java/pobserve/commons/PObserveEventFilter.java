package pobserve.commons;

import java.io.Serializable;
import java.util.function.Function;

public interface PObserveEventFilter<E> extends Function<PObserveEvent, Boolean>, Serializable {}
