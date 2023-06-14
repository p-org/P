package psym.runtime.values;

public abstract class PCollection extends PValue<PCollection> {
    public abstract int size();

    public abstract boolean contains(PValue<?> item);
}
