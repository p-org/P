package psymbolic.valuesummary;

import org.jetbrains.annotations.NotNull;

import java.util.*;

/**
 * Represents a value of "any" type
 * It stores a pair (type T, value of type T)
 * */
@SuppressWarnings("ALL")
public class UnionVS implements ValueSummary<UnionVS> {
    /* Type of value stored in the any type variable */
    private final PrimitiveVS<Class<? extends ValueSummary>> type;
    /* Map from the type of variable to the value summary representing the value of that type */
    private Map<Class<? extends ValueSummary>, ValueSummary> value;

    public UnionVS(@NotNull PrimitiveVS<Class<? extends ValueSummary>> type, @NotNull Map<Class<? extends ValueSummary>, ValueSummary> values) {
        this.type = type;
        this.value = values;
    }

    public UnionVS(Guard pc, Class<? extends ValueSummary> type, ValueSummary values) {
        this.type = new PrimitiveVS<Class<? extends ValueSummary>>(type).restrict(pc);
        this.value = new HashMap<>();
        // TODO: why are we not restricting the values?
        this.value.put(type, values);
        assert(this.type != null);
    }

    public UnionVS() {
        this.type = new PrimitiveVS<>();
        this.value = new HashMap<>();
    }

    public UnionVS(UnionVS vs) {
        this.type = new PrimitiveVS<>(vs.type);
        this.value = new HashMap<>(vs.value);
    }

    public UnionVS(ValueSummary vs) {
        this(vs.getUniverse(), vs.getClass(), vs);
    }

    /**
     * Does the any type variable store a value of a particular type
     * @param queryType type to be checked
     * @return true if the variable stores a value of "queryType" under some path constrain
     */
    public boolean hasType(Class<? extends ValueSummary> queryType) {
        return !type.getGuardFor(queryType).isFalse();
    }

    /**
     * Get the types of value stored in the "any" type variable
     * @return type of the variable
     */
    public PrimitiveVS<Class<? extends ValueSummary>> getType() {
        return type;
    }

    /**
     * Get the value in the of a particular type
     * @param type type of value
     * @return value
     */
    public ValueSummary getValue(Class<? extends ValueSummary> type) {
        // TODO: Add a check that the type exists!
        return value.get(type);
    }

    public Guard getGuardFor(Class<? extends ValueSummary> type) {
        return this.type.getGuardFor(type);
    }

    public void check() {
        for (Class<? extends ValueSummary> type : this.type.getValues()) {
            assert getGuardFor(type).isFalse() || (getValue(type) != null);
        }
    }

    @Override
    public boolean isEmptyVS() {
        return type.isEmptyVS();
    }

    @Override
    public UnionVS restrict(Guard guard) {

        if(guard.equals(getUniverse()))
            return new UnionVS(this);

        final PrimitiveVS<Class<? extends ValueSummary>> restrictedType = type.restrict(guard);
        final Map<Class<? extends ValueSummary>, ValueSummary> restrictedValues = new HashMap<>();
        for (Map.Entry<Class<? extends ValueSummary>, ValueSummary> entry : value.entrySet()) {
            final Class<? extends ValueSummary> type = entry.getKey();
            final ValueSummary value = entry.getValue();
            if (!restrictedType.getGuardFor(type).isFalse()) {
                restrictedValues.put(type, value.restrict(guard));
            }
        }
        return new UnionVS(restrictedType, restrictedValues);
    }

    @Override
    public UnionVS merge(Iterable<UnionVS> summaries) {
        assert(type != null);
        final List<PrimitiveVS<Class<? extends ValueSummary>>> typesToMerge = new ArrayList<>();
        final Map<Class<? extends ValueSummary>, List<ValueSummary>> valuesToMerge = new HashMap<>();
        for (UnionVS union : summaries) {
            typesToMerge.add(union.type);
            for (Map.Entry<Class<? extends ValueSummary>, ValueSummary> entry : union.value.entrySet()) {
                valuesToMerge
                        .computeIfAbsent(entry.getKey(), (key) -> new ArrayList<>())
                        .add(entry.getValue());
            }
        }

        if (valuesToMerge.size() == 0) return new UnionVS();

        final PrimitiveVS<Class<? extends ValueSummary>> mergedType = type.merge(typesToMerge);
        final Map<Class<? extends ValueSummary>, ValueSummary> mergedValue = new HashMap<>(this.value);

        for (Map.Entry<Class<? extends ValueSummary>, List<ValueSummary>> entry : valuesToMerge.entrySet()) {
            Class<? extends ValueSummary> type = entry.getKey();
            List<ValueSummary> value = entry.getValue();
            if (value.size() > 0) {
                ValueSummary oldValue = this.value.get(type);
                ValueSummary newValue;
                if (oldValue == null) {
                    newValue = value.get(0).merge(value.subList(1, entry.getValue().size()));
                } else {
                    newValue = oldValue.merge(value);
                }
                mergedValue.put(type, newValue);
            }
        }
        return new UnionVS(mergedType, mergedValue);
    }

    @Override
    public UnionVS merge(UnionVS summary) {
        return merge(Collections.singletonList(summary));
    }

    @Override
    public UnionVS combineVals(UnionVS other) {
        // TODO: figure out how to support
        throw new RuntimeException("Predicates for any types not supported!");
    }

    @Override
    public UnionVS updateUnderGuard(Guard guard, UnionVS updateVal) {
        return this.restrict(guard.not()).merge(updateVal.restrict(guard));
    }

    @Override
    public PrimitiveVS<Boolean> symbolicEquals(UnionVS cmp, Guard pc) {
        assert(type != null);
        PrimitiveVS res = type.symbolicEquals(cmp.type, pc);
        for (Map.Entry<Class<? extends ValueSummary>, ValueSummary> payload : cmp.value.entrySet()) {
            if (!value.containsKey(payload.getKey())) {
                PrimitiveVS<Boolean> bothLackKey = BooleanVS.trueUnderGuard(pc.and(type.getGuardFor(payload.getKey()).not()));
                res = BooleanVS.and(res, bothLackKey);
            } else {
                res = BooleanVS.and(res, payload.getValue().symbolicEquals(value.get(payload.getKey()), pc));
            }
        }
        return res;
    }

    @Override
    public Guard getUniverse() {
        return type.getUniverse();
    }

    public Guard getUniverse(Class<? extends ValueSummary> type) { return this.type.getGuardFor(type); }

    @Override
    public String toString() {
        StringBuilder out = new StringBuilder();
        out.append("[");
        for (Class<? extends ValueSummary>type : type.getValues()) {
            out.append(value.get(type).toString());
            out.append(", ");
        }
        out.append("]");
        return out.toString();
    }

}
