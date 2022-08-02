package testcases;

import prt.values.PValue;

import java.math.BigInteger;

// A simple foreign type that wraps a BigInteger.
public class UnboundedNat implements PValue<UnboundedNat> {
    private BigInteger i = BigInteger.ZERO;
    public BigInteger getI() {
        return i;
    }

    public void add(long l) {
        i = i.add(BigInteger.valueOf(l));
    }

    @Override
    public UnboundedNat deepClone() {
        UnboundedNat n = new UnboundedNat();
        n.i = i;
        return n;
    }

    @Override
    public boolean deepEquals(UnboundedNat o2) {
        return o2 != null && this.i.equals(o2.i);
    }
}
