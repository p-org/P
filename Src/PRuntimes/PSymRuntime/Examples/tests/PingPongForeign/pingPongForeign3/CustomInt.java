package psym.model;

import lombok.Getter;
import psym.runtime.values.*;

public class CustomInt {
    @Getter
    private int value;

    CustomInt(int val) {
        this.value = val;
    }
}
