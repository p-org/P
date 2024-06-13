package pexplicit.runtime.scheduler.choice;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;

@Getter
@Setter
@AllArgsConstructor
public abstract class Choice<T> implements Serializable {
    protected T value;
}
