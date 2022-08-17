package sample.sampleimpl;

import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

public class Ring {
    private byte val; // What will happen on overflow??? uh oh

    private final Logger logger = LogManager.getLogger(this.getClass());

    public Ring() {
        val = 0;
    }

    public void Add(int i) {
        val += i;
        logger.info(String.format("ADD:%d,%d", i, val));
    }

    public void Mul(int i) {
        val *= i;
        logger.info(String.format("MUL:%d,%d", i, val));
    }

    public byte getVal() { return val; }

}
