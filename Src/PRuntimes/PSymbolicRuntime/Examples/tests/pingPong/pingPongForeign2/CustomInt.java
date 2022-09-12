package psymbolic;

import lombok.Getter;
import p.runtime.values.*;

public class CustomInt {
    @Getter
    private int value;

    CustomInt(int val) {
        this.value = val;
    }
}
