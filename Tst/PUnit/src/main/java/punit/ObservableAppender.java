package punit;

import io.reactivex.rxjava3.core.Observable;
import io.reactivex.rxjava3.subjects.PublishSubject;
import org.apache.logging.log4j.core.Filter;
import org.apache.logging.log4j.core.LogEvent;
import org.apache.logging.log4j.core.appender.AbstractAppender;

/**
 * An ObservableAppender consumes Log4J Log events and published them
 * for downstream Observers to consume.
 */
public class ObservableAppender extends AbstractAppender {

    private final PublishSubject<String> downstream;


    public ObservableAppender(Filter filter) {
        // TODO: Layout?
        super("ObservableAppender", filter, null, false, null);
        this.downstream = PublishSubject.create();
    }

    @Override
    public void append(LogEvent event) {
        downstream.onNext(event.getMessage().getFormattedMessage());
    }

    public Observable<String> observe() {
        return downstream;
    }
}
