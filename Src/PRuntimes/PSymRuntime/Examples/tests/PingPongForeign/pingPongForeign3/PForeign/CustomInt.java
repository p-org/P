package psym.model;

import lombok.Getter;
import psym.runtime.values.*;

public class CustomInt {
    @Getter
    private int value;

    CustomInt(int val) {
        this.value = (int) val;
    }

    CustomInt(Object val) {
        if (val instanceof CustomInt)
            this.value = ((CustomInt) val).getValue();
        else
            this.value = (int) val;
    }
}
