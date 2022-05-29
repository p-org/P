package pcontainment;

import com.microsoft.z3.*;
import lombok.Getter;
import p.runtime.values.*;
import pcontainment.runtime.Message;
import pcontainment.runtime.Payloads;
import pcontainment.runtime.machine.*;
import pcontainment.runtime.machine.eventhandlers.EventHandler;
import pcontainment.runtime.machine.eventhandlers.EventHandlerReturnReason;

import java.util.*;
import java.util.stream.Collectors;

public class Checker {
    private final Context ctx = new Context();
    private final Solver solver = ctx.mkSolver();
    private final List<BoolExpr> hasSendPreds = new ArrayList<>();
    private final List<IntExpr> sendTgtIds = new ArrayList<>();
    private final List<Payloads> payloads = new ArrayList<>();
    private final List<Message> concreteSends = new ArrayList<>();
    private IntExpr currentState = ctx.mkIntConst("state_-1");
    private IntExpr nextState = ctx.mkIntConst("state_0");
    private Locals locals = new Locals();
    private int localMergeCount = 0;

    public Expr<?> getExprFor(PValue<?> value) {
        if (value instanceof PInt) {
            return ctx.mkInt(((PInt) value).getValue());
        } else if (value instanceof PFloat) {
            return ctx.mkReal(((Double) ((PFloat) value).getValue()).toString());
        } else if (value instanceof PBool) {
           return ctx.mkBool(((PBool) value).getValue());
        } else if (value instanceof PString) {
            return ctx.mkString(((PString) value).getValue());
        } else {
            throw new RuntimeException("Unflattened composite datatypes in local variables.");
        }
    }

    public Expr<?> getConstWithSameType(Expr<?> expr, String name) {
        if (expr == null) {
            return ctx.mkInt(-1);
        }
        if (expr instanceof IntExpr) {
            return ctx.mkIntConst(name);
        } else if (expr instanceof RealExpr) {
            return ctx.mkRealConst(name);
        } else if (expr instanceof BoolExpr) {
            return ctx.mkBoolConst(name);
        } else if (expr.isString()) {
            return mkStringConst(name);
        } else if (expr instanceof SeqExpr<?>) {
            // TODO: handle other kinds of sequences?
            return ctx.mkEmptySeq(ctx.getIntSort());
        }
        else {
            throw new RuntimeException("Cannot make const of sort " + expr.getSort().toString());
        }
    }

    public void declLocal(String name, Object default_value) {
        Expr<?> val = null;
        if (default_value instanceof PValue) {
            PValue<?> value = (PValue<?>) default_value;
            val = getExprFor(value);
            locals.put(name, getConstWithSameType(val, name + "_" + depth));
            solver.add(mkEq(val, locals.get(name)));
        } else if (default_value instanceof Expr) {
            val = (Expr<?>) default_value;
            locals.put(name, getConstWithSameType(val, name + "_" + depth));
            solver.add(mkEq(val, locals.get(name)));
        } else {
            locals.put(name, mkInt(-1));
        }
    }

    private Expr<?> getCurrentLocal(String name) {
        return getLocal(name, getDepth());
    }

    private Expr<?> getNextLocal(String name) {
        return getLocal(name, getDepth() + 1);
    }

    private Expr<?> getLocal(String name, int depth) {
        if (!locals.containsKey(name))
            throw new RuntimeException("Tried to access undeclared local: " + name + "");
        if (depth != getDepth()) {
            if (locals.get(name) == null) return null;
            System.out.println("getLocal is bool?:" + locals.get(name).isBool());
            return getConstWithSameType(locals.get(name), name + "_" + depth);
        }
        return locals.get(name);
    }

    private List<Expr<?>> getNextLocals() {
        List<Expr<?>> nextLocals = new ArrayList<>();
        for (String localName : locals.keySet()) {
            System.out.println(localName + ": " + getNextLocal(localName));
            nextLocals.add(getNextLocal(localName));
        }
        return nextLocals;
    }

    @Getter
    private int depth = -1;

    public IntExpr getStateEncoding(State s) { return ctx.mkInt(s.getId()); }

    public IntExpr mkInt(int i) { return ctx.mkInt(i); }

    public IntExpr mkIntConst(String s) { return ctx.mkIntConst(s); }

    public IntExpr mkFreshIntConst() { return (IntExpr) ctx.mkFreshConst("fresh", ctx.getIntSort()); }

    public RealExpr mkReal(String s) { return ctx.mkReal(s); }

    public RealExpr mkRealConst(String s) { return ctx.mkRealConst(s); }

    public RealExpr mkFreshRealConst() { return (RealExpr) ctx.mkFreshConst("fresh", ctx.getRealSort()); }

    public BoolExpr mkBool(boolean b) { return ctx.mkBool(b); }

    public BoolExpr mkBoolConst(String s) { return ctx.mkBoolConst(s); }

    public BoolExpr mkFreshBoolConst() { return (BoolExpr) ctx.mkFreshConst("fresh", ctx.getBoolSort()); }

    public SeqExpr<CharSort> mkString(String s) { return ctx.mkString(s); }

    public Expr<SeqSort<CharSort>> mkStringConst(String s) { return ctx.mkConst(ctx.mkSymbol(s), ctx.mkStringSort()); }

    public ArithExpr<?> mkPlus(ArithExpr<?> e1, ArithExpr<?> e2) { return ctx.mkAdd(e1, e2); }

    public ArithExpr<?> mkMinus(ArithExpr<?> e1, ArithExpr<?> e2) { return ctx.mkSub(e1, e2); }

    public ArithExpr<?> mkTimes(ArithExpr<?> e1, ArithExpr<?> e2) { return ctx.mkMul(e1, e2); }

    public ArithExpr<?> mkDiv(ArithExpr<?> e1, ArithExpr<?> e2) { return ctx.mkDiv(e1, e2); }

    public BoolExpr mkGt(ArithExpr<?> e1, ArithExpr<?> e2) { return ctx.mkGt(e1, e2); }

    public BoolExpr mkEq(Expr<?> e1, Expr<?> e2) { return ctx.mkEq(e1, e2); }

    public BoolExpr mkImplies(BoolExpr e1, BoolExpr e2) { return ctx.mkImplies(e1, e2); }

    public BoolExpr mkAnd(BoolExpr e1, BoolExpr e2) { return ctx.mkAnd(e1, e2); }

    public BoolExpr mkOr(BoolExpr e1, BoolExpr e2) { return ctx.mkOr(e1, e2); }

    public BoolExpr mkNot(BoolExpr e) { return ctx.mkNot(e); }

    public Expr<?> mkITE(BoolExpr e1, Expr<?> e2, Expr<?> e3) {
        return ctx.mkITE(e1, e2, e3);
    }

    //TODO: support other types of sequences?
    public SeqExpr<IntSort> mkSeq() {
        return ctx.mkEmptySeq(ctx.getIntSort());
    }

    public BoolExpr send(int sends, Message m) {
        if (sends == hasSendPreds.size()) {
            hasSendPreds.add(ctx.mkBoolConst("send_" + depth + "_" + sends));
            sendTgtIds.add(ctx.mkIntConst("tgt_" + depth + "_" + sends));
            payloads.add(new Payloads());
        } else if (sends > hasSendPreds.size()) {
            throw new RuntimeException("Sends exceeds number of send predicates by 1.");
        }
        BoolExpr hasSend = hasSendPreds.get(sends);
        BoolExpr targetEq = ctx.mkEq(sendTgtIds.get(sends), ((SymbolicMachineIdentifier) m.getTargetId()).id);
        BoolExpr payloadsEq = ctx.mkTrue();
        for (Map.Entry<String, Object> entry : m.payloads.entrySet()) {
            if (!(entry.getValue() instanceof Expr)) {
                /*
                System.out.println("entry key: " + entry.getKey());
                System.out.println("entry value?" + entry.getValue().getClass());
                System.out.println("entry value: " + entry.getValue().toString());
                 */
                throw new RuntimeException("Compiled payload in send not symbolic");
            }
            if (!payloads.get(sends).containsField(entry.getKey())) {
                String varName = entry.getKey() + "_" + depth + "_" + sends;
                if (entry.getValue() instanceof BoolExpr) {
                    payloads.get(sends).put(entry.getKey(), mkBoolConst(varName));
                } else if (entry.getValue() instanceof IntExpr) {
                    payloads.get(sends).put(entry.getKey(), mkIntConst(varName));
                } else if (entry.getValue() instanceof RealExpr) {
                    payloads.get(sends).put(entry.getKey(), mkRealConst(varName));
                } else {
                    payloads.get(sends).put(entry.getKey(), mkStringConst(varName));
                }
            }
            BoolExpr eqExpr;
            if (entry.getValue() instanceof BoolExpr && payloads.get(sends).get(entry.getKey()) instanceof BoolExpr) {
                eqExpr = ctx.mkEq((BoolExpr) entry.getValue(), (BoolExpr) payloads.get(sends).get(entry.getKey()));
            } else if (entry.getValue() instanceof IntExpr &&
                       payloads.get(sends).get(entry.getKey()) instanceof IntExpr) {
                eqExpr = ctx.mkEq((IntExpr) entry.getValue(), (IntExpr) payloads.get(sends).get(entry.getKey()));
            } else if (entry.getValue() instanceof RealExpr &&
                       payloads.get(sends).get(entry.getKey()) instanceof RealExpr) {
                eqExpr = ctx.mkEq((RealExpr) entry.getValue(), (RealExpr) payloads.get(sends).get(entry.getKey()));
            } else if (entry.getValue() instanceof SeqExpr &&
                       payloads.get(sends).get(entry.getKey()) instanceof SeqExpr) {
                eqExpr = ctx.mkEq((SeqExpr<?>) entry.getValue(), (SeqExpr<?>) entry.getValue());
            } else {
                throw new RuntimeException("Symbolic payload and variable have mismatched types");
            }
            payloadsEq = ctx.mkAnd(payloadsEq, eqExpr);
        }
        return ctx.mkAnd(hasSend, targetEq, payloadsEq);
    }

    public void started() {
        depth++;
        currentState = nextState;
        nextState = ctx.mkIntConst("state_" + (depth + 1));
        for (String k : locals.keySet()) {
            locals.put(k, getConstWithSameType(locals.get(k), k + "_" + depth));
        }
    }

    public void nextDepth() {
        depth++;
        currentState = nextState;
        nextState = ctx.mkIntConst("state_" + (depth + 1));
        hasSendPreds.clear();
        sendTgtIds.clear();
        payloads.clear();
        concreteSends.clear();
        for (String k : locals.keySet()) {
            locals.put(k, getConstWithSameType(locals.get(k), k + "_" + depth));
        }
    }

    // encode payload in receive
    public Payloads encodeConcretePayload(Payloads pld) {
        Payloads encoded = new Payloads();
        for (Map.Entry<String, Object> entry : pld.entrySet()) {
            if (entry.getValue() instanceof ConcreteMachineIdentifier) {
                encoded.put(entry.getKey(), mkInt(((ConcreteMachineIdentifier) entry.getValue()).id));
            } else {
                encoded.put(entry.getKey(), getExprFor((PValue<?>) entry.getValue()));
            }
        }
        return encoded;
    }

    // encode payload in send - assumes handlers already ran
    private List<BoolExpr> encodeConcretePayload(Payloads pld, int sendIdx) {
        List<BoolExpr> payloadExprs = new ArrayList<>();
        Payloads payloadFields = payloads.get(sendIdx);
        for (Map.Entry<String, Object> entry : pld.entrySet()) {
            String fieldName = entry.getKey();
            if (payloadFields.containsField(fieldName)) {
                Object payloadFieldExpr = payloadFields.get(fieldName);
                Object payloadFieldValue = entry.getValue();
                if (payloadFieldValue instanceof PInt) {
                    if (payloadFieldExpr instanceof IntExpr) {
                        payloadExprs.add(
                                ctx.mkEq((IntExpr) payloadFieldExpr, mkInt(((PInt) payloadFieldValue).getValue())));
                    } else {
                        throw new RuntimeException("Mismatched payload types");
                    }
                } else if (payloadFieldValue instanceof PFloat) {
                    if (payloadFieldExpr instanceof RealExpr) {
                        payloadExprs.add(ctx.mkEq((RealExpr) payloadFieldExpr, mkReal(payloadFieldValue.toString())));
                    } else {
                        throw new RuntimeException("Mismatched payload types");
                    }
                } else if (payloadFieldValue instanceof PBool) {
                    if (payloadFieldExpr instanceof BoolExpr) {
                        payloadExprs.add(
                            ctx.mkEq((BoolExpr) payloadFieldExpr, mkBool(((PBool) payloadFieldValue).getValue())));
                    } else {
                        throw new RuntimeException("Mismatched payload types");
                    }
                } else if (payloadFieldValue instanceof PString) {
                    if (payloadFieldExpr instanceof SeqExpr) {
                        payloadExprs.add(
                            ctx.mkEq((SeqExpr) payloadFieldExpr,
                                    mkString(((PString) payloadFieldValue).getValue())));
                    } else {
                        throw new RuntimeException("Mismatched payload types");
                    }
                } else {
                    throw new RuntimeException("Unflattened composite datatypes in payload: " + fieldName + ": " + payloadFieldValue);
                }
            } else {
                throw new RuntimeException("Mismatched payload fields");
            }
        }
        return payloadExprs;
    }

    // assumes handlers already ran
    public void addConcreteSend(Message s) {
        concreteSends.add(s);
        if (hasSendPreds.size() <= concreteSends.size()) {
            int sendIdx = concreteSends.size() - 1;
            BoolExpr exists = hasSendPreds.get(sendIdx);
            BoolExpr tgt = ctx.mkEq(sendTgtIds.get(sendIdx),
                    ctx.mkInt(((ConcreteMachineIdentifier) s.getTargetId()).id));
            List<BoolExpr> payload = encodeConcretePayload(s.payloads, sendIdx);
            solver.add(exists, tgt);
            for (BoolExpr pldExpr : payload) {
                solver.add(pldExpr);
            }
        } else {
            throw new RuntimeException("Implementation has more sends than model");
        }
    }

    public void noMoreSends() {
        for (int i = concreteSends.size(); i < hasSendPreds.size(); i++) {
            solver.add(ctx.mkNot(hasSendPreds.get(i)));
        }
    }

    public void check() {
        BoolExpr[] asserts = solver.getAssertions();
        /*
        for (BoolExpr asst : asserts) {
            System.out.println(asst.simplify().toString());
        }
         */
        Status res = solver.check();
        if (res != Status.SATISFIABLE) {
            throw new RuntimeException("Trace containment check failed: " + res.name());
        }
        Model m = solver.getModel();
        FuncDecl<?>[] decls = m.getConstDecls();
        boolean addOld = false;
        List<BoolExpr> boolExprs = new ArrayList<>();
        Expr<?> interp = m.getConstInterp(nextState);
        if (interp != null) {
            BoolExpr eq = ctx.mkEq(nextState, interp);
            if (solver.check(ctx.mkNot(eq)) != Status.SATISFIABLE) {
                System.out.println("Value for state " + nextState + " is unique!! " + interp.toString());
                boolExprs.add(eq);
            } else {
                addOld = true;
            }
        } else {
            addOld = true;
        }
        for (Expr<?> e : getNextLocals()) {
            if (e == null) continue;
            interp = m.getConstInterp(e);
            if (interp == null) continue;
            BoolExpr eq = ctx.mkEq(e, interp);
            if (solver.check(ctx.mkNot(eq)) != Status.SATISFIABLE) {
                //System.out.println("Value for " + e.toString() + " is unique!!");
                boolExprs.add(eq);
            } else {
                addOld = true;
            }
        }
        /*
        for (BoolExpr hasSendPred : hasSendPreds) {
            BoolExpr eq = ctx.mkEq(hasSendPred, m.getConstInterp(hasSendPred));
            if (solver.check(ctx.mkNot(eq)) != Status.SATISFIABLE) {
                boolExprs.add(eq);
            } else {
                addOld = true;
            }
        }
         */
        /*
        for (IntExpr sendTgtId : sendTgtIds) {
            BoolExpr eq = ctx.mkEq(sendTgtId, m.getConstInterp(sendTgtId));
            if (solver.check(ctx.mkNot(eq)) != Status.SATISFIABLE) {
                boolExprs.add(eq);
            } else {
                addOld = true;
            }
        }
         */
        //solver = ctx.mkSolver();
        if (addOld) {
            solver.reset();
            for (BoolExpr a : asserts) {
                solver.add(a);
            }
        } else {
            solver.reset();
            for (BoolExpr a : boolExprs) {
                solver.add(a);
            }
        }
    }

    private IntExpr getCurrentState(int n) {
        if (n == 0) return currentState;
        return ctx.mkIntConst("state_" + depth + "_" + n);
    }

    public void runOutcomesToCompletion(Machine target, EventHandlerReturnReason returnReason) {
        Pair<BoolExpr, Locals> encoding = encodeOutcomesToCompletion(0, 0, locals, target, returnReason);
        locals = encoding.second;
        solver.add(encoding.first.simplify());
    }

    private Pair<BoolExpr, Locals> encodeOutcomesToCompletion(int sends, int states, Locals locals,
                                                Machine target, EventHandlerReturnReason returnReason) {
        if (returnReason instanceof EventHandlerReturnReason.Raise) {
            return encodeOutcomesToCompletion(sends, states, locals, target,
                    (EventHandlerReturnReason.Raise) returnReason);
        } else if (returnReason instanceof EventHandlerReturnReason.Goto) {
            return encodeOutcomesToCompletion(sends, states, locals, target,
                    (EventHandlerReturnReason.Goto) returnReason);
        } else { // Normal Return
            Pair<BoolExpr, Locals> frame = frameRule(locals);
            return new Pair<>(ctx.mkAnd(ctx.mkEq(nextState, getCurrentState(states)), frame.first), frame.second);
        }
    }

    private Pair<BoolExpr, Locals> frameRule(Map<String, Expr<?>> locals) {
        Locals newLocals = new Locals();
        BoolExpr[] eqs = new BoolExpr[locals.size()];
        int i = 0;
        for (Map.Entry<String, Expr<?>> entry : locals.entrySet()) {
            System.out.println("local " + entry.getKey());
            Expr<?> nextLocal = getNextLocal(entry.getKey());
            System.out.println("current is bool?: " + entry.getValue().isBool());
            System.out.println("next is bool?: " + nextLocal.isBool());
            eqs[i] = ctx.mkEq(entry.getValue(), nextLocal);
            newLocals.put(entry.getKey(), nextLocal);
            i++;
        }
        return new Pair<>(ctx.mkAnd(eqs), newLocals);
    }

    private Pair<BoolExpr, Locals> encodeOutcomesToCompletion(int sends, int states, Locals locals,
                                                Machine target, EventHandlerReturnReason.Raise raise) {
        BoolExpr outcomes = ctx.mkFalse();
        System.out.println("encode outcome for " + raise.getMessage().getEvent().toString());
        Message m = raise.getMessage();
        Iterable<EventHandler> eventHandlers = target.getHandlersFor(m.getEvent());
        IntExpr startState = getCurrentState(states);
        Set<Pair<BoolExpr, Locals>> localsToMerge = new HashSet<>();
        for (EventHandler handler : eventHandlers) {
            BoolExpr stateExpr = ctx.mkEq(startState, ctx.mkInt(handler.state.getId()));
            Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> encoding =
                    handler.getEncoding(sends, locals, target, m.getPayloads());
            Pair<BoolExpr, Locals> outcome = runEncoding(target, states, encoding);
            outcomes = ctx.mkOr(outcomes, ctx.mkAnd(stateExpr, outcome.first));
            localsToMerge.add(new Pair<>(outcome.first, outcome.second));
        }
        return new Pair<>(outcomes, mergeLocals(localsToMerge));
    }

    private Locals mergeLocals(Set<Pair<BoolExpr, Locals>> localsSet) {
        Locals mergedLocalMap= new Locals();
        for (Map.Entry<String, Expr<?>> entry: locals.entrySet()) {
            String key = entry.getKey();
            String mergedName = key + "_" + depth + "_merge_" + localMergeCount;
            Expr<?> mergedVal = getConstWithSameType(entry.getValue(), mergedName);
            mergedLocalMap.put(key, mergedVal);
            BoolExpr mergedEncoding = ctx.mkTrue();
            for (Pair<BoolExpr, Locals> locals : localsSet) {
                if (locals.second.containsKey(key)) {
                    mergedEncoding = mkOr(mergedEncoding,
                            mkAnd(locals.first, mkEq(mergedVal, locals.second.get(key))));
                }
            }
            solver.add(mergedEncoding);
        }
        localMergeCount++;
        return mergedLocalMap;
    }

    private Pair<BoolExpr, Locals> encodeOutcomesToCompletion(int sends, int states, Locals locals,
                                                Machine target, EventHandlerReturnReason.Goto goTo) {
        Map<BoolExpr, Integer> sendCounts = new HashMap<>();
        Locals mergedLocals = new Locals();
        for (Map.Entry<String, Expr<?>> entry: locals.entrySet()) {
            String key = entry.getKey();
            String mergedName = key + "_" + depth + "_" + states;
            mergedLocals.put(key, getConstWithSameType(entry.getValue(), mergedName));
        }
        for (State state : target.getStates()) {
            BoolExpr stateExit = ctx.mkEq(getCurrentState(states), ctx.mkInt(state.getId()));
            Map<BoolExpr, Pair<Integer, Locals>> exitRes =
                    state.getExitEncoding(sends, locals, this, target);
            BoolExpr branches = ctx.mkFalse();
            for (Map.Entry<BoolExpr, Pair<Integer, Locals>> branch : exitRes.entrySet()) {
                sendCounts.put(ctx.mkAnd(stateExit, branch.getKey()), branch.getValue().first);
                BoolExpr merge = ctx.mkTrue();
                for (Map.Entry<String, Expr<?>> entry : branch.getValue().second.entrySet()) {
                    merge = ctx.mkAnd(merge, ctx.mkEq(entry.getValue(),
                                                      mergedLocals.get(entry.getKey())));
                }
                branches = ctx.mkOr(branches, ctx.mkAnd(branch.getKey(), merge));
            }
        }
        states++;
        BoolExpr entries = ctx.mkFalse();
        System.out.println("state update for " + getCurrentState(states).toString());
        System.out.println("equal to " + goTo.getGoTo().getId());
        BoolExpr stateUpdate = ctx.mkEq(getCurrentState(states), ctx.mkInt(goTo.getGoTo().getId()));
        Set<Pair<BoolExpr, Locals>> localsToMerge = new HashSet<>();
        for (Map.Entry<BoolExpr, Integer> branch : sendCounts.entrySet()) {
            Pair<BoolExpr, Locals> thisEntry = runEncoding(target, states,
                                                goTo.getGoTo().getEntryEncoding(branch.getValue(),this, mergedLocals,
                                                target, goTo.getPayloads()));
            entries = ctx.mkOr(entries, ctx.mkAnd(branch.getKey(), thisEntry.first));
            localsToMerge.add(new Pair<>(branch.getKey(), thisEntry.second));
        }
        return new Pair<>(mkAnd(entries, stateUpdate), mergeLocals(localsToMerge));
    }

    private Pair<BoolExpr, Locals> runEncoding(Machine target, int states, Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> encoding) {
        BoolExpr outcomeEncoding = ctx.mkFalse();
        Set<Pair<BoolExpr, Locals>> localsToMerge = new HashSet<>();
        for (Map.Entry<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> branch : encoding.entrySet()) {
            Pair<BoolExpr, Locals> outcome = encodeOutcomesToCompletion(branch.getValue().first, states,
                    branch.getValue().second, target,
                    branch.getValue().third);
            outcomeEncoding = ctx.mkOr(outcome.first, ctx.mkAnd(branch.getKey(), outcome.first));
            localsToMerge.add(new Pair<>(branch.getKey(), outcome.second));
        }
        return new Pair<>(outcomeEncoding, mergeLocals(localsToMerge));
    }

}
