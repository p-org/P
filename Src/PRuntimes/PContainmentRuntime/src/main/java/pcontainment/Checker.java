package pcontainment;

import com.microsoft.z3.*;
import p.runtime.values.PBool;
import p.runtime.values.PInt;
import p.runtime.values.PSeq;
import p.runtime.values.PString;
import pcontainment.runtime.Message;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

public class Checker {
    private final Context ctx = new Context();
    private final Solver solver = ctx.mkSolver();
    // TODO: move
    List<BoolExpr> hasSendPreds = new ArrayList<>();
    List<IntExpr> sendTgtIds = new ArrayList<>();
    List<Map<String, Object>> payloads = new ArrayList<>();
    List<Message> concreteSends = new ArrayList<>();

    // assumes handlers already ran
    private List<BoolExpr> encodeConcretePayload(Map<String, Object> pld, int sendIdx) {
        List<BoolExpr> payloadExprs = new ArrayList<>();
        Map<String, Object> payloadFields = payloads.get(sendIdx);
        for (Map.Entry<String, Object> entry : pld.entrySet()) {
            String fieldName = entry.getKey();
            if (payloadFields.containsKey(fieldName)) {
                Object payloadFieldExpr = payloadFields.get(fieldName);
                Object payloadFieldValue = entry.getValue();
                if (payloadFieldValue instanceof PInt) {
                    if (payloadFieldExpr instanceof IntExpr) {
                        payloadExprs.add(
                                ctx.mkEq((IntExpr) payloadFieldExpr, ctx.mkInt(((PInt) payloadFieldValue).getValue())));
                    } else {
                        throw new RuntimeException("Mismatched payload types");
                    }
                } else if (payloadFieldValue instanceof PBool) {
                    if (payloadFieldExpr instanceof BoolExpr) {
                        payloadExprs.add(
                            ctx.mkEq((BoolExpr) payloadFieldExpr, ctx.mkBool(((PBool) payloadFieldValue).getValue())));
                    } else {
                        throw new RuntimeException("Mismatched payload types");
                    }
                } else if (payloadFieldValue instanceof PString) {
                    if (payloadFieldExpr instanceof SeqExpr) {
                        payloadExprs.add(
                            ctx.mkEq((SeqExpr) payloadFieldExpr,
                                    ctx.mkString(((PString) payloadFieldValue).getValue())));
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

    public void addConcreteSend(Message s) {
        concreteSends.add(s);
        if (hasSendPreds.size() <= concreteSends.size()) {
            int sendIdx = concreteSends.size() - 1;
            BoolExpr exists = hasSendPreds.get(sendIdx);
            BoolExpr tgt = ctx.mkEq(sendTgtIds.get(sendIdx), ctx.mkInt(s.getTarget()));
            List<BoolExpr> payload = encodeConcretePayload(s.payloads, sendIdx);
            solver.add(exists, tgt);
            for (BoolExpr pldExpr : payload) {
                solver.add(pldExpr);
            }
            solver.push();
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
}
