package pcontainment.runtime.machine;

import com.microsoft.z3.*;
import org.jetbrains.annotations.NotNull;
import p.runtime.values.*;
import pcontainment.Checker;

import java.util.*;

import static pcontainment.runtime.machine.ExprConcretizer.concretize;
import static pcontainment.runtime.machine.ExprConcretizer.isConcrete;

public class Locals {

    public static final boolean CONCRETE = false;
    public static final boolean ARRAY_THEORY = true;
    public static final boolean SEQ_THEORY = true;
    public static final boolean SET_THEORY = false;
    public static final boolean FLATTEN = false;

    private final Checker checker;

    private final Map<String, PBool> concrete_bools = new HashMap<>();
    private final Map<String, PInt> concrete_ints = new HashMap<>();
    private final Map<String, PFloat> concrete_floats = new HashMap<>();
    private final Map<String, PString> concrete_strings = new HashMap<>();
    private final Map<String, PSeq> concrete_seqs = new HashMap<>();
    private final Map<String, PMap> concrete_maps = new HashMap<>();
    private final Map<String, PSet> concrete_sets = new HashMap<>();

    private final Map<String, BoolExpr> symbolic_bools = new HashMap<>();
    private final Map<String, IntExpr> symbolic_ints = new HashMap<>();
    private final Map<String, RealExpr> symbolic_floats = new HashMap<>();
    private final Map<String, SeqExpr<CharSort>> symbolic_strings = new HashMap<>();
    private final Map<String, SeqExpr<?>> symbolic_seqs = new HashMap<>();
    private final Map<String, ArrayExpr<?, ?>> symbolic_maps = new HashMap<>();
    private final Map<String, Expr<SetSort<?>>> symbolic_sets = new HashMap<>();

    private final Set<String> seqs = new HashSet<>();
    private final Set<String> maps = new HashSet<>();
    private final Set<String> sets = new HashSet<>();

    private static String getFlattenedMapElt(String mapName, PValue<?> val) {
        return mapName + "_key=" + val.toString();
    }

    private static String getFlattenedSeqElt(String seqName, int idx) {
        return seqName + "_" + idx;
    }

    private static String getFlattenedSeqSize(String seqName) {
        return seqName + "_size";
    }

    private static String getMapKeys(String mapName) {
        return mapName + "_keySet";
    }

    public Locals(Checker checker) { this.checker = checker; }

    public Locals(Locals other) {
        concrete_bools.putAll(other.concrete_bools);
        concrete_ints.putAll(other.concrete_ints);
        concrete_floats.putAll(other.concrete_floats);
        concrete_strings.putAll(other.concrete_strings);
        concrete_seqs.putAll(other.concrete_seqs);
        concrete_maps.putAll(other.concrete_maps);
        concrete_sets.putAll(other.concrete_sets);
        symbolic_bools.putAll(other.symbolic_bools);
        symbolic_ints.putAll(other.symbolic_ints);
        symbolic_floats.putAll(other.symbolic_floats);
        symbolic_strings.putAll(other.symbolic_strings);
        symbolic_seqs.putAll(other.symbolic_seqs);
        symbolic_maps.putAll(other.symbolic_maps);
        symbolic_sets.putAll(other.symbolic_sets);
        seqs.addAll(other.seqs);
        maps.addAll(other.maps);
        sets.addAll(other.sets);
        checker = other.checker;
    }

    public boolean hasSeq(String name) {
        return seqs.contains(name);
    }

    public Locals immutableAssign(String key, Expr<?> value) {
        Locals ret = new Locals(this);
        ret.assign(key, value);
        return ret;
    }

    public Locals immutableAssign(String key, PValue<?> value) {
        Locals ret = new Locals(this);
        ret.assign(key, value);
        return ret;
    }

    public Locals immutablePut(String map, Expr<?> key, Expr<?> value) {
        Locals ret = new Locals(this);
        ret.put(map, key, value);
        return ret;
    }

    public Locals immutableAdd(String seq, Expr<?> value) {
        Locals ret = new Locals(this);
        ret.add(seq, value);
        return ret;
    }

    public Locals immutableSubseq(String seq, int i) {
        Locals ret = new Locals(this);
        ret.subseq(seq, i);
        return ret;
    }

    public PValue<?> getConcrete(String seq, int idx) {
        if (CONCRETE && concrete_seqs.containsKey(seq)) {
            return concrete_seqs.get(seq).getValue(idx);
        }
        if (FLATTEN) {
            String flattenedKey = getFlattenedSeqElt(seq, idx);
            return getConcrete(flattenedKey);
        }
        return null;
    }

    public Expr<?> get(String seq, int idx) {
        if (CONCRETE && concrete_seqs.containsKey(seq)) {
            return checker.getExprFor(concrete_seqs.get(seq).getValue(idx));
        }
        if (SEQ_THEORY && symbolic_seqs.containsKey(seq)) {
            return checker.mkGet(symbolic_seqs.get(seq), checker.mkInt(idx));
        }
        if (FLATTEN) {
            String flattenedKey = getFlattenedSeqElt(seq, idx);
            return get(flattenedKey);
        }
        throw new RuntimeException("Cannot get index " + idx + " of sequence " + seq);
    }

    // put into key-value store
    private void put(String map, Expr<?> key, Expr<?> value) {
        if (!maps.contains(map))
            throw new RuntimeException(("Cannot put into nonexistent map " + map));
        if (CONCRETE && concrete_maps.containsKey(map)) {
            if (isConcrete(key) && isConcrete(value)) {
                concrete_maps.get(map).putValue(concretize(key), concretize(value));
            } else {
                convertConcreteToSymbolic(map);
                put(map, key, value);
            }
        }
        if (FLATTEN) {
            if (isConcrete(key) && (!ARRAY_THEORY || !symbolic_maps.containsKey(map))) {
                put(map, concretize(key), value);
                return;
            }
        }
        if (ARRAY_THEORY) {
            assign(map, checker.mkAdd((ArrayExpr) symbolic_maps.get(map), key, value));
        }
    }

    // put concrete key into key-value store
    private void put(String map, PValue<?> key, Expr<?> value) {
        if (!maps.contains(map))
            throw new RuntimeException(("Cannot put into nonexistent map " + map));
        if (CONCRETE && concrete_maps.containsKey(map)) {
            if (isConcrete(value)) {
                concrete_maps.get(map).putValue(key, concretize(value));
            } else {
                convertConcreteToSymbolic(map);
                put(map, key, value);
            }
        }
        if (FLATTEN) {
            if (!ARRAY_THEORY || !symbolic_maps.containsKey(map)) {
                assign(getFlattenedMapElt(map, key), value);
                return;
            }
        }
        if (ARRAY_THEORY) {
            assign(map, checker.mkAdd((ArrayExpr) symbolic_maps.get(map),
                    checker.getExprFor(key), value));
        }
    }

    // add a concrete value
    private void add(String seq, PValue<?> value) {
        if (CONCRETE && concrete_seqs.containsKey(seq)) {
            concrete_seqs.get(seq).insertValue(concrete_seqs.get(seq).size(), value);
        } else {
            // add symbolic expression
            add(seq, checker.getExprFor(value));
        }
    }

    // add symbolic value
    private void add(String seq, Expr<?> value) {
        if (CONCRETE && concrete_seqs.containsKey(seq)) {
            throw new RuntimeException("Conversion from concrete sequences to symbolic not yet implemented");
        }
        if (SEQ_THEORY && symbolic_seqs.containsKey(seq)) {
            symbolic_seqs.put(seq, checker.mkAdd(symbolic_seqs.get(seq), value));
        }
        if (FLATTEN) {
            int idx = concrete_ints.get(getFlattenedSeqSize(seq)).getValue();
            String flattenedKey = getFlattenedSeqElt(seq, idx);
            assign(flattenedKey, value);
        } else {
            throw new RuntimeException("Could not add value " + value + " to sequence " + seq);
        }
    }

    private void subseq(String seq, int idx) {
        if (CONCRETE && concrete_seqs.containsKey(seq)) {
            List<PValue<?>> subList = new ArrayList<>();
            PSeq fullSeq = concrete_seqs.get(seq);
            for (int i = idx; i < fullSeq.size(); i++) {
                subList.add(fullSeq.getValue(i));
            }
            concrete_seqs.put(seq, new PSeq(subList));
        }
        else if (SEQ_THEORY && symbolic_seqs.containsKey(seq)) {
            symbolic_seqs.put(seq, checker.mkSubseq(symbolic_seqs.get(seq), checker.mkInt(idx)));
        }
        else if (FLATTEN) {
            int size = concrete_ints.get(getFlattenedSeqSize(seq)).getValue();
            for (int i = idx; i < size; i++) {
                String flattenedKey = getFlattenedSeqElt(seq, idx);
                PValue<?> concreteVal = getConcrete(seq, i);
                if (concreteVal != null) assign(flattenedKey, concreteVal);
                else assign(flattenedKey, get(seq, i));
            }
        } else {
            throw new RuntimeException("Could not get subsequence of " + seq);
        }
    }

    public boolean contains(String name) {
        return concrete_bools.containsKey(name) || concrete_ints.containsKey(name) ||
               concrete_floats.containsKey(name) || concrete_strings.containsKey(name) ||
               symbolic_bools.containsKey(name) || symbolic_ints.containsKey(name) ||
               symbolic_floats.containsKey(name) || symbolic_strings.containsKey(name) ||
               seqs.contains(name) || sets.contains(name) || maps.contains(name);
    }

    public PValue<?> getConcrete(String name) {
        if (CONCRETE) {
            if (concrete_bools.containsKey(name)) {
                return concrete_bools.get(name);
            }
            if (concrete_ints.containsKey(name)) {
                return concrete_ints.get(name);
            }
            if (concrete_floats.containsKey(name)) {
                return concrete_floats.get(name);
            }
            if (concrete_strings.containsKey(name)) {
                return concrete_strings.get(name);
            }
            if (concrete_seqs.containsKey(name)) {
                return concrete_seqs.get(name);
            }
            if (concrete_sets.containsKey(name)) {
                return concrete_sets.get(name);
            }
            if (concrete_maps.containsKey(name)) {
                return concrete_maps.get(name);
            }
        }
        return null;
    }

    public Expr<?> get(String name) {
        if (CONCRETE) {
            if (concrete_bools.containsKey(name)) {
                return checker.getExprFor(concrete_bools.get(name));
            }
            if (concrete_ints.containsKey(name)) {
                return checker.getExprFor(concrete_ints.get(name));
            }
            if (concrete_floats.containsKey(name)) {
                return checker.getExprFor(concrete_floats.get(name));
            }
            if (concrete_strings.containsKey(name)) {
                return checker.getExprFor(concrete_strings.get(name));
            }
            if (concrete_seqs.containsKey(name)) {
                return checker.getExprFor(concrete_seqs.get(name));
            }
            if (concrete_sets.containsKey(name)) {
                return checker.getExprFor(concrete_sets.get(name));
            }
            if (concrete_maps.containsKey(name)) {
                return checker.getExprFor(concrete_maps.get(name));
            }
        }
        if (symbolic_bools.containsKey(name)) {
            return symbolic_bools.get(name);
        }
        if (symbolic_ints.containsKey(name)) {
            return symbolic_ints.get(name);
        }
        if (symbolic_floats.containsKey(name)) {
            return symbolic_floats.get(name);
        }
        if (symbolic_strings.containsKey(name)) {
            return symbolic_strings.get(name);
        }
        if (SEQ_THEORY && symbolic_seqs.containsKey(name)) {
            return symbolic_seqs.get(name);
        }
        if (ARRAY_THEORY && symbolic_maps.containsKey(name)) {
            return symbolic_maps.get(name);
        }
        if (SET_THEORY && symbolic_sets.containsKey(name)) {
            return symbolic_sets.get(name);
        }
        if (seqs.contains(name)) {
            throw new RuntimeException("Conversion from flattened sequences not yet implemented: cannot get " + name);
        }
        if (maps.contains(name)) {
            throw new RuntimeException("Conversion from flattened maps not yet implemented: cannot get " + name);
        }
        if (sets.contains(name)) {
            throw new RuntimeException("Conversion from flattened sets not yet implemented: cannot get " + name);
        }
        throw new RuntimeException("Cannot get " + name + ": not in set of locals");
    }

    public void assign(String varName, Expr<?> value) {
        if (CONCRETE && isConcrete(value)) {
            assign(varName, concretize(value));
        }
        remove(varName);
        if (value.isBool()) {
            symbolic_bools.put(varName, (BoolExpr) value);
        } else if (value instanceof IntExpr) {
            symbolic_ints.put(varName, (IntExpr) value);
        } else if (value instanceof RealExpr) {
            symbolic_floats.put(varName, (RealExpr) value);
        } else if (value.isString()) {
            symbolic_strings.put(varName, (SeqExpr<CharSort>) value);
        } else if (value instanceof SeqExpr<?>) {
            if (SEQ_THEORY) {
                symbolic_seqs.put(varName, (SeqExpr<?>) value);
                seqs.add(varName);
            } else {
                throw new RuntimeException("Cannot assign sequence with sequence theory disabled");
            }
        } else if (value.getSort() instanceof SetSort<?>) {
            throw new RuntimeException("No support for set sorts");
        } else if (value instanceof ArrayExpr<?, ?>) {
            if (ARRAY_THEORY) {
                symbolic_maps.put(varName, (ArrayExpr<?, ?>) value);
                maps.add(varName);
            } else {
                throw new RuntimeException("Cannot assign sequence with array theory disabled");
            }
        } else {
            throw new RuntimeException("Cannot assign expression of sort " + value.getSort());
        }
    }

    public void assign(String varName, PValue<?> value) {
        if (CONCRETE) {
            remove(varName);
            if (value instanceof PBool) {
                concrete_bools.put(varName, (PBool) value);
            } else if (value instanceof PInt) {
                concrete_ints.put(varName, (PInt) value);
            } else if (value instanceof PFloat) {
                concrete_floats.put(varName, (PFloat) value);
            } else if (value instanceof PString) {
                concrete_strings.put(varName, (PString) value);
            } else if (value instanceof PSeq) {
                concrete_seqs.put(varName, (PSeq) value);
            } else if (value instanceof PSet) {
                concrete_sets.put(varName, (PSet) value);
            } else {
                concrete_maps.put(varName, (PMap) value);
            }
        } else {
            assign(varName, checker.getExprFor(value));
        }
    }

    public void remove(String name) {
        concrete_bools.remove(name);
        concrete_ints.remove(name);
        concrete_floats.remove(name);
        concrete_strings.remove(name);
        symbolic_bools.remove(name);
        symbolic_ints.remove(name);
        symbolic_floats.remove(name);
        symbolic_strings.remove(name);
        if (seqs.remove(name) && FLATTEN) {
            String sizeName = getFlattenedSeqSize(name);
            int size = concrete_ints.get(sizeName).getValue();
            for (int i = 0; i < size; i++) {
                remove(getFlattenedSeqElt(name, i));
            }
            remove(sizeName);
        }
        if (maps.remove(name)) {
            String keySetName = getMapKeys(name);
            if (FLATTEN && concrete_seqs.containsKey(keySetName)) {
                PSet keySet = concrete_sets.get(keySetName);
                // remove every key's value
                for (int i = 0; i < keySet.size(); i++) {
                    remove(getFlattenedMapElt(name, keySet.getValue(i)));
                }
            }
            remove(keySetName);
        }
        concrete_sets.remove(name);
    }

    public Interpretation getInterpretation(Model model) {
        return new Interpretation(model, symbolic_bools, symbolic_ints, symbolic_floats,
                symbolic_strings, symbolic_seqs, symbolic_maps, symbolic_sets);
    }

    public void determinize() {
        Model model = checker.getLastModel();
        if (model == null) return;

        // get current model interpretations
        Interpretation interp = new Interpretation(model, symbolic_bools, symbolic_ints, symbolic_floats,
                symbolic_strings, symbolic_seqs, symbolic_maps, symbolic_sets);

        // negate each boolean and solve again
        for (String var : symbolicVarSet()) {
            checker.mkNot(checker.mkEq(get(var), interp.getInterpretation(var)));
        }
        // unsat - this assignment is constant

        // sat - remove this from constants (it's not constant)
        // check if anything else has changed

        //
        // remove any that don't match

    }

    @NotNull
    public Set<String> symbolicVarSet() {
        Set<String> keySet = new HashSet<>();
        keySet.addAll(symbolic_bools.keySet());
        keySet.addAll(symbolic_ints.keySet());
        keySet.addAll(symbolic_floats.keySet());
        keySet.addAll(symbolic_strings.keySet());
        keySet.addAll(symbolic_seqs.keySet());
        keySet.addAll(symbolic_maps.keySet());
        keySet.addAll(symbolic_sets.keySet());
        return keySet;
    }

    @NotNull
    public Set<String> concreteVarSet() {
        Set<String> keySet = new HashSet<>();
        keySet.addAll(concrete_bools.keySet());
        keySet.addAll(concrete_ints.keySet());
        keySet.addAll(concrete_floats.keySet());
        keySet.addAll(concrete_strings.keySet());
        keySet.addAll(concrete_seqs.keySet());
        keySet.addAll(concrete_maps.keySet());
        keySet.addAll(concrete_sets.keySet());
        return keySet;
    }

    @NotNull
    public Set<String> seqSet() {
        return seqs;
    }

    @NotNull
    public Set<String> setSet() {
        return sets;
    }

    @NotNull
    public Set<String> mapSet() {
        return maps;
    }

    public void convertConcreteToSymbolic() {
        for (String str : concreteVarSet()) {
            assign(str, get(str));
        }
    }

    public void convertConcreteToSymbolic(String varName) {
        assign(varName, get(varName));
    }

    public void unflatten(String str) {
        if (FLATTEN) {
            throw new RuntimeException("Unflattening not yet supported");
        }
    }
}
