package pcontainment.runtime.machine;

import com.microsoft.z3.Expr;
import lombok.Getter;

import java.util.ArrayList;

public class DeterministicSeq<T extends Expr<?>> {
    @Getter
    private final String name;
    private ArrayList<T> seq = new ArrayList<>();

    public DeterministicSeq(String name) {
        this.name = name;
    }

    public DeterministicSeq(DeterministicSeq<T> other) {
        this.name = other.name;
        seq = new ArrayList<>(other.seq);
    }

    public DeterministicSeq<T> immutableAdd(T toAdd) {
        DeterministicSeq<T> ret = new DeterministicSeq<>(this);
        ret.add(toAdd);
        return ret;
    }

    public DeterministicSeq<T> immutableAdd(int idx, T toAdd) {
        DeterministicSeq<T> ret = new DeterministicSeq<>(this);
        ret.add(idx, toAdd);
        return ret;
    }

    public DeterministicSeq<T> immutableSubseq(int idx) {
        DeterministicSeq<T> ret = new DeterministicSeq<>(this);
        ret.seq = (ArrayList<T>) ret.seq.subList(idx, ret.seq.size());
        return ret;
    }

    public int size() {
        return seq.size();
    }

    private boolean add(T toAdd) {
        return seq.add(toAdd);
    }

    private void add (int idx, T toPut) {
        seq.add(idx, toPut);
    }

    public T get (int idx) {
        return seq.get(idx);
    }

}
