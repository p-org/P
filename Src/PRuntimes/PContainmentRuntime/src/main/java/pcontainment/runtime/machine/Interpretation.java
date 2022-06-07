package pcontainment.runtime.machine;

import com.microsoft.z3.*;
import p.runtime.values.exceptions.KeyNotFoundException;

import java.util.HashMap;
import java.util.Map;

public class Interpretation {
    private final Map<String, BoolExpr> interp_bools = new HashMap<>();
    private final Map<String, IntExpr> interp_ints = new HashMap<>();
    private final Map<String, RealExpr> interp_floats = new HashMap<>();
    private final Map<String, SeqExpr<CharSort>> interp_strings = new HashMap<>();
    private final Map<String, SeqExpr> interp_seqs = new HashMap<>();
    private final Map<String, Expr<SetSort<?>>> interp_sets = new HashMap<>();
    private final Map<String, ArrayExpr<?, ?>> interp_maps = new HashMap<>();

    public Expr<?> getInterpretation(String str) {
        if (interp_bools.containsKey(str)) return interp_bools.get(str);
        if (interp_ints.containsKey(str)) return interp_ints.get(str);
        if (interp_floats.containsKey(str)) return interp_floats.get(str);
        if (interp_strings.containsKey(str)) return interp_strings.get(str);
        if (interp_seqs.containsKey(str)) return interp_seqs.get(str);
        if (interp_sets.containsKey(str)) return interp_sets.get(str);
        if (interp_maps.containsKey(str)) return interp_maps.get(str);
        throw new KeyNotFoundException("No interpretation for " + str);
    }

    public Interpretation(Model model,
                          Map<String, BoolExpr> symbolic_bools, Map<String, IntExpr> symbolic_ints,
                          Map<String, RealExpr> symbolic_floats, Map<String, SeqExpr<CharSort>> symbolic_strings,
                          Map<String, SeqExpr<?>> symbolic_seqs, Map<String, ArrayExpr<?, ?>> symbolic_maps,
                          Map<String, Expr<SetSort<?>>> symbolic_sets) {
        for (Map.Entry<String, BoolExpr> entry : symbolic_bools.entrySet()) {
            BoolExpr res = (BoolExpr) model.getConstInterp(entry.getValue());
            interp_bools.put(entry.getKey(), res);
        }
        for (Map.Entry<String, IntExpr> entry : symbolic_ints.entrySet()) {
            IntExpr res = (IntExpr) model.getConstInterp(entry.getValue());
            interp_ints.put(entry.getKey(), res);
        }
        for (Map.Entry<String, RealExpr> entry : symbolic_floats.entrySet()) {
            RealExpr res = (RealExpr) model.getConstInterp(entry.getValue());
            interp_floats.put(entry.getKey(), res);
        }
        for (Map.Entry<String, SeqExpr<CharSort>> entry : symbolic_strings.entrySet()) {
            SeqExpr<CharSort> res = (SeqExpr<CharSort>) model.getConstInterp(entry.getValue());
            interp_strings.put(entry.getKey(), res);
        }
        for (Map.Entry<String, SeqExpr<?>> entry : symbolic_seqs.entrySet()) {
            SeqExpr<?> res = (SeqExpr) model.getConstInterp(entry.getValue());
            interp_seqs.put(entry.getKey(), res);
        }
        for (Map.Entry<String, Expr<SetSort<?>>> entry : symbolic_sets.entrySet()) {
            Expr<SetSort<?>> res = model.getConstInterp(entry.getValue());
            interp_sets.put(entry.getKey(), res);
        }
        for (Map.Entry<String, ArrayExpr<?, ?>> entry : symbolic_maps.entrySet()) {
            ArrayExpr<?,?> res = (ArrayExpr) model.getConstInterp(entry.getValue());
            interp_maps.put(entry.getKey(), res);
        }
    }
}
