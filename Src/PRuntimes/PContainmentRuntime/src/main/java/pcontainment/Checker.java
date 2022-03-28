package pcontainment;

import com.microsoft.z3.*;
import lombok.Getter;
import p.runtime.values.*;
import pcontainment.runtime.Message;
import pcontainment.runtime.Payloads;
import pcontainment.runtime.machine.Machine;
import pcontainment.runtime.machine.State;
import pcontainment.runtime.machine.eventhandlers.EventHandler;
import pcontainment.runtime.machine.eventhandlers.EventHandlerReturnReason;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class Checker {
    private final Context ctx = new Context();
    private final Solver solver = ctx.mkSolver();
    private final List<BoolExpr> hasSendPreds = new ArrayList<>();
    private final List<IntExpr> sendTgtIds = new ArrayList<>();
    private final List<Payloads> payloads = new ArrayList<>();
    private final List<Message> concreteSends = new ArrayList<>();
    private IntExpr currentState = ctx.mkIntConst("state_0");
    @Getter
    private int depth = 0;

    public BoolExpr getCurrentStateEq(State s) { return ctx.mkEq(currentState, getStateEncoding(s)); }

    public IntExpr getStateEncoding(State s) { return ctx.mkInt(s.getId()); }

    public IntExpr mkInt(int i) { return ctx.mkInt(i); }

    public IntExpr mkIntConst(String s) { return ctx.mkIntConst(s); }

    public RealExpr mkReal(String s) { return ctx.mkReal(s); }

    public RealExpr mkRealConst(String s) { return ctx.mkRealConst(s); }

    public BoolExpr mkBool(boolean b) { return ctx.mkBool(b); }

    public BoolExpr mkBoolConst(String s) { return ctx.mkBoolConst(s); }

    public SeqExpr<CharSort> mkString(String s) { return ctx.mkString(s); }

    public Expr<SeqSort<CharSort>> mkStringConst(String s) { return ctx.mkConst(ctx.mkSymbol(s), ctx.mkStringSort()); }

    public ArithExpr<?> mkPlus(ArithExpr<?> e1, ArithExpr<?> e2) { return ctx.mkAdd(e1, e2); }

    public ArithExpr<?> mkMinus(ArithExpr<?> e1, ArithExpr<?> e2) { return ctx.mkSub(e1, e2); }

    public ArithExpr<?> mkTimes(ArithExpr<?> e1, ArithExpr<?> e2) { return ctx.mkMul(e1, e2); }

    public ArithExpr<?> mkDiv(ArithExpr<?> e1, ArithExpr<?> e2) { return ctx.mkDiv(e1, e2); }

    public BoolExpr mkEq(Expr<?> e1, Expr<?> e2) { return ctx.mkEq(e1, e2); }

    public BoolExpr mkAnd(BoolExpr e1, BoolExpr e2) { return ctx.mkAnd(e1, e2); }

    public BoolExpr mkOr(BoolExpr e1, BoolExpr e2) { return ctx.mkOr(e1, e2); }

    public BoolExpr mkNot(BoolExpr e) { return ctx.mkNot(e); }

    public BoolExpr send(int sends, Message m) {
        if (sends == hasSendPreds.size()) {
            hasSendPreds.add(ctx.mkBoolConst("send_" + depth + "_" + sends));
            sendTgtIds.add(ctx.mkIntConst("tgt_" + depth + "_" + sends));
            payloads.add(new Payloads());
        } else if (sends > hasSendPreds.size()) {
            throw new RuntimeException("Sends exceeds number of send predicates by 1.");
        }
        BoolExpr hasSend = hasSendPreds.get(sends);
        BoolExpr targetEq = ctx.mkEq(sendTgtIds.get(sends), ctx.mkInt(m.getTarget().getId()));
        BoolExpr payloadsEq = ctx.mkTrue();
        for (Map.Entry<String, Object> entry : m.payloads.entrySet()) {
            if (!(entry.getValue() instanceof Expr)) {
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

    public void nextDepth() {
        depth++;
        currentState = ctx.mkIntConst("state_" + depth);
        hasSendPreds.clear();
        sendTgtIds.clear();
        payloads.clear();
        concreteSends.clear();
    }

    // assumes handlers already ran
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
                    throw new RuntimeException("Unflattened composite datatypes in payload: " + payloadFieldValue);
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
            BoolExpr tgt = ctx.mkEq(sendTgtIds.get(sendIdx), ctx.mkInt(s.getTarget().getId()));
            List<BoolExpr> payload = encodeConcretePayload(s.payloads, sendIdx);
            solver.add(exists, tgt);
            for (BoolExpr pldExpr : payload) {
                solver.add(pldExpr);
            }
            solver.push();
        } else {
            throw new RuntimeException("Implementation has more sends than model");
        }
    }

    public void noMoreSends() {
        for (int i = concreteSends.size(); i < hasSendPreds.size(); i++) {
            solver.add(ctx.mkNot(hasSendPreds.get(i)));
            solver.push();
        }
    }

    public void check() {
        Status res = solver.check();
        if (res != Status.SATISFIABLE) {
            throw new RuntimeException("Trace containment check failed: " + res.name());
        }
    }

    public void runOutcomesToCompletion(Machine target, EventHandlerReturnReason returnReason) {
        BoolExpr encoding = encodeOutcomesToCompletion(0, target, returnReason);
        solver.add(encoding);
    }

    private BoolExpr encodeOutcomesToCompletion(int sends, Machine target, EventHandlerReturnReason returnReason) {
        if (returnReason instanceof EventHandlerReturnReason.Raise) {
            return encodeOutcomesToCompletion(sends, target, (EventHandlerReturnReason.Raise) returnReason);
        } else if (returnReason instanceof EventHandlerReturnReason.Goto) {
            return encodeOutcomesToCompletion(sends, target, (EventHandlerReturnReason.Goto) returnReason);
        } else { // Normal Return
            return ctx.mkTrue();
        }
    }

    private BoolExpr encodeOutcomesToCompletion(int sends, Machine target, EventHandlerReturnReason.Raise raise) {
        BoolExpr outcomes = ctx.mkFalse();
        Message m = raise.getMessage();
        Iterable<EventHandler> eventHandlers = target.getHandlersFor(m.getEvent());
        int i = 0;
        for (EventHandler handler : eventHandlers) {
            Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> encoding = handler.getEncoding(sends, target, m.getPayloads());
            BoolExpr outcome = runEncoding(target, encoding);
            outcomes = ctx.mkOr(outcomes, outcome);
            i++;
        }
        return outcomes;
    }

    private BoolExpr encodeOutcomesToCompletion(int sends, Machine target, EventHandlerReturnReason.Goto goTo) {
        Map<BoolExpr, Integer> sendCounts = new HashMap<>();
        for (State state : target.getStates()) {
            BoolExpr stateExit = ctx.mkEq(currentState, ctx.mkInt(state.getId()));
            Map<BoolExpr, Integer> exitRes = state.getExitEncoding(sends, this, target);
            BoolExpr branches = ctx.mkFalse();
            for (Map.Entry<BoolExpr, Integer> branch : exitRes.entrySet()) {
                sendCounts.put(ctx.mkAnd(stateExit, branch.getKey()), branch.getValue());
                branches = ctx.mkOr(branches, branch.getKey());
            }
        }
        BoolExpr entries = ctx.mkFalse();
        for (Map.Entry<BoolExpr, Integer> branch : sendCounts.entrySet()) {
            BoolExpr thisEntry = runEncoding(target, goTo.getGoTo().getEntryEncoding(branch.getValue(), this, target));
            entries = ctx.mkOr(entries, ctx.mkAnd(branch.getKey(), thisEntry));
        }
        return entries;
    }

    private BoolExpr runEncoding(Machine target, Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> encoding) {
        BoolExpr outcome = ctx.mkFalse();
        for (Map.Entry<BoolExpr, Pair<Integer, EventHandlerReturnReason>> branch : encoding.entrySet()) {
            outcome = ctx.mkOr(outcome, ctx.mkAnd(branch.getKey(),
                               encodeOutcomesToCompletion(branch.getValue().first, target, branch.getValue().second)));
        }
        return outcome;
    }

}
