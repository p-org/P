package psymbolic.valuesummary;

import psymbolic.valuesummary.bdd.Bdd;

import java.util.*;

/** Class for union value summaries */
public class UnionVS implements ValueSummary<UnionVS> {
    private PrimitiveVS<Class<? extends ValueSummary>> type;
    private Map<Class<? extends ValueSummary>, ValueSummary> payloads;

    public UnionVS(PrimVS<Class<? extends ValueSummary>> type, Map<Class<? extends ValueSummary>, ValueSummary> payloads) {
        this.type = type;
        this.payloads = payloads;
        assert(this.type != null);
    }

    public UnionVS(Bdd pc, Class<? extends ValueSummary> type, ValueSummary payloads) {
        this.type = new PrimVS<Class<? extends ValueSummary>>(type).guard(pc);
        this.payloads = new HashMap<>();
        this.payloads.put(type, payloads);
        assert(this.type != null);
    }

    public UnionVS() {
        this.type = new PrimVS<>();
        this.payloads = new HashMap<>();
        assert(this.type != null);
    }

    public UnionVS(UnionVS vs) {
        this.type = vs.type;
        this.payloads = new HashMap<>(vs.payloads);
    }

    public UnionVS(ValueSummary vs) {
        this(vs.getUniverse(), vs.getClass(), vs);
    }

    public boolean hasType(Class<? extends ValueSummary> queryType) {
        return !type.getGuard(queryType).isConstFalse();
    }

    public PrimVS<Class<? extends ValueSummary>> getType() {
        return type;
    }

    public ValueSummary getPayload(Class<? extends ValueSummary> type) {
        return payloads.get(type);
    }

    public void check() {
        for (Class<? extends ValueSummary> typeOpt : type.getValues()) {
            if (!getUniverse(typeOpt).isConstFalse()) {
                assert(getPayload(typeOpt) != null);
            }
        }
    }

    @Override
    public boolean isEmptyVS() {
        return type.isEmptyVS();
    }

    @Override
    public UnionVS guard(Bdd guard) {
        assert(type != null);
        final PrimVS<Class<? extends ValueSummary>> newTag = type.guard(guard);
        final Map<Class<? extends ValueSummary>, ValueSummary> newPayloads = new HashMap<>();
        for (Map.Entry<Class<? extends ValueSummary>, ValueSummary> entry : payloads.entrySet()) {
            final Class<? extends ValueSummary> tag = entry.getKey();
            final ValueSummary value = entry.getValue();
            if (!newTag.getGuard(tag).isConstFalse()) {
                newPayloads.put(tag, value.guard(guard));
            }
        }
        return new UnionVS(newTag, newPayloads);
    }

    @Override
    public UnionVS merge(Iterable<UnionVS> summaries) {
        assert(type != null);
        final List<PrimVS<Class<? extends ValueSummary>>> tagsToMerge = new ArrayList<>();
        final Map<Class<? extends ValueSummary>, List<ValueSummary>> valuesToMerge = new HashMap<>();
        for (UnionVS union : summaries) {
            tagsToMerge.add(union.type);
            for (Map.Entry<Class<? extends ValueSummary>, ValueSummary> entry : union.payloads.entrySet()) {
                valuesToMerge
                        .computeIfAbsent(entry.getKey(), (key) -> new ArrayList<>())
                        .add(entry.getValue());
            }
        }

        if (valuesToMerge.size() == 0) return new UnionVS();

        final PrimVS<Class<? extends ValueSummary>> newTag = type.merge(tagsToMerge);
        final Map<Class<? extends ValueSummary>, ValueSummary> newPayloads = new HashMap<>(this.payloads);

        for (Map.Entry<Class<? extends ValueSummary>, List<ValueSummary>> entry : valuesToMerge.entrySet()) {
            Class<? extends ValueSummary> tag = entry.getKey();
            List<ValueSummary> entryPayload = entry.getValue();
            if (entryPayload.size() > 0) {
                ValueSummary oldPayload = this.payloads.get(tag);
                ValueSummary newPayload;
                if (oldPayload == null) {
                    newPayload = entryPayload.get(0).merge(entryPayload.subList(1, entry.getValue().size()));
                } else {
                    newPayload = oldPayload.merge(entryPayload);
                }
                newPayloads.put(tag, newPayload);
            }
        }

        UnionVS res = new UnionVS(newTag, newPayloads);
        return res;
    }

    @Override
    public UnionVS merge(UnionVS summary) {
        return merge(Collections.singletonList(summary));
    }

    @Override
    public UnionVS update(Bdd guard, UnionVS update) {
        return this.guard(guard.not()).merge(update.guard(guard));
    }

    @Override
    public PrimVS<Boolean> symbolicEquals(UnionVS cmp, Bdd pc) {
        assert(type != null);
        PrimVS<Boolean> res = type.symbolicEquals(cmp.type, pc);
        for (Map.Entry<Class<? extends ValueSummary>, ValueSummary> payload : cmp.payloads.entrySet()) {
            if (!payloads.containsKey(payload.getKey())) {
                PrimVS<Boolean> bothLackKey = BoolUtils.fromTrueGuard(pc.and(type.getGuard(payload.getKey()).not()));
                res = BoolUtils.and(res, bothLackKey);
            } else {
                res = BoolUtils.and(res, payload.getValue().symbolicEquals(payloads.get(payload.getKey()), pc));
            }
        }
        return res;
    }

    @Override
    public Bdd getUniverse() {
        return type.getUniverse();
    }

    public Bdd getUniverse(Class<? extends ValueSummary> type) { return this.type.getGuard(type); }

    @Override
    public String toString() {
        return type.toString();
    }

}
